# 考试次数限制UI集成文档

## 概述

本文档描述了Examina.Desktop项目中考试次数限制功能在UI层的集成实现，包括ViewModel增强和View更新。

## ViewModel集成 - ExamViewModel

### 新增属性

#### 考试管理属性
```csharp
[Reactive] public ObservableCollection<StudentExamDto> AvailableExams { get; set; }
[Reactive] public StudentExamDto? SelectedExam { get; set; }
[Reactive] public ExamAttemptLimitDto? ExamAttemptLimit { get; set; }
[Reactive] public ObservableCollection<ExamAttemptDto> ExamAttemptHistory { get; set; }
[Reactive] public ExamAttemptDto? CurrentExamAttempt { get; set; }
```

#### 权限控制属性
```csharp
[Reactive] public bool CanRetake { get; set; }
[Reactive] public bool CanPractice { get; set; }
```

#### 显示属性
```csharp
public string RetakeButtonText => ExamAttemptLimit?.RemainingRetakeCount > 0 
    ? $"重考 (剩余{ExamAttemptLimit.RemainingRetakeCount}次)" 
    : "重考";

public string ExamStatusDescription => ExamAttemptLimit?.StatusDisplay ?? "未知状态";
public string AttemptCountDescription => ExamAttemptLimit?.AttemptCountDisplay ?? "";
```

### 新增命令

#### 考试操作命令
```csharp
public ICommand RetakeExamCommand { get; }      // 重考命令
public ICommand PracticeExamCommand { get; }    // 重做练习命令
public ICommand SelectExamCommand { get; }      // 选择考试命令
public ICommand ViewExamHistoryCommand { get; } // 查看考试历史命令
```

#### 命令实现
```csharp
RetakeExamCommand = new DelegateCommand(RetakeExam, CanRetakeExam);
PracticeExamCommand = new DelegateCommand(PracticeExam, CanPracticeExam);
SelectExamCommand = new DelegateCommand<StudentExamDto>(SelectExam);
ViewExamHistoryCommand = new DelegateCommand(ViewExamHistory, CanViewExamHistory);
```

### 核心方法

#### 考试选择处理
```csharp
private async Task OnSelectedExamChanged(StudentExamDto? exam)
{
    // 检查考试次数限制
    ExamAttemptLimit = await _examAttemptService.CheckExamAttemptLimitAsync(exam.Id, studentId);
    
    // 更新按钮状态
    CanRetake = ExamAttemptLimit.CanRetake;
    CanPractice = ExamAttemptLimit.CanPractice;
    
    // 加载考试历史
    List<ExamAttemptDto> history = await _examAttemptService.GetExamAttemptHistoryAsync(exam.Id, studentId);
    ExamAttemptHistory.Clear();
    foreach (ExamAttemptDto attempt in history)
    {
        ExamAttemptHistory.Add(attempt);
    }
}
```

#### 考试开始流程
```csharp
private async Task StartExamAttempt(ExamAttemptType attemptType)
{
    // 验证权限
    (bool isValid, string? errorMessage) = await _examAttemptService
        .ValidateExamAttemptPermissionAsync(SelectedExam.Id, studentId, attemptType);
    
    if (isValid)
    {
        // 开始考试尝试
        ExamAttemptDto? attempt = await _examAttemptService
            .StartExamAttemptAsync(SelectedExam.Id, studentId, attemptType);
        
        if (attempt != null)
        {
            CurrentExamAttempt = attempt;
            HasActiveExam = true;
            ExamStatus = "考试进行中";
            
            // 启动考试工具栏
            await StartExamToolbarAsync(SelectedExam, attemptType);
        }
    }
}
```

## View集成 - ExamView.axaml

### 新增UI组件

#### 1. 考试选择卡片
```xml
<!-- 考试选择卡片 -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumLowBrush}"
        CornerRadius="12" Padding="24">
  <StackPanel Spacing="16">
    <!-- 考试选择标题 -->
    <StackPanel Orientation="Horizontal" Spacing="12">
      <TextBlock Text="📝" FontSize="32"/>
      <StackPanel VerticalAlignment="Center">
        <TextBlock Text="选择考试" FontSize="20" FontWeight="SemiBold"/>
        <TextBlock Text="请选择要参加的考试" FontSize="14"/>
      </StackPanel>
    </StackPanel>
    
    <!-- 考试列表 -->
    <ComboBox ItemsSource="{Binding AvailableExams}"
              SelectedItem="{Binding SelectedExam}"
              DisplayMemberBinding="{Binding Name}"
              PlaceholderText="请选择考试"/>
    
    <!-- 考试次数限制信息 -->
    <StackPanel IsVisible="{Binding SelectedExam, Converter={x:Static ObjectConverters.IsNotNull}}">
      <Grid>
        <Grid.ColumnDefinitions>
          <ColumnDefinition Width="*"/>
          <ColumnDefinition Width="*"/>
        </Grid.ColumnDefinitions>
        
        <StackPanel Grid.Column="0">
          <TextBlock Text="考试状态"/>
          <TextBlock Text="{Binding ExamStatusDescription}"/>
        </StackPanel>
        
        <StackPanel Grid.Column="1">
          <TextBlock Text="次数统计"/>
          <TextBlock Text="{Binding AttemptCountDescription}"/>
        </StackPanel>
      </Grid>
    </StackPanel>
  </StackPanel>
</Border>
```

#### 2. 增强的操作按钮
```xml
<!-- 操作按钮 -->
<StackPanel Spacing="12">
  <!-- 主要操作按钮 -->
  <StackPanel Orientation="Horizontal" Spacing="12" HorizontalAlignment="Center">
    <Button Content="{Binding StartExamButtonText}"
            Classes="accent"
            Command="{Binding StartExamCommand}"
            IsEnabled="{Binding SelectedExam, Converter={x:Static ObjectConverters.IsNotNull}}"/>
    
    <Button Content="继续考试"
            Classes="accent"
            Command="{Binding ContinueExamCommand}"
            IsVisible="{Binding HasActiveExam}"/>
    
    <Button Content="刷新状态"
            Command="{Binding RefreshExamStatusCommand}"/>
  </StackPanel>
  
  <!-- 重考和练习按钮 -->
  <StackPanel Orientation="Horizontal" Spacing="12" HorizontalAlignment="Center"
              IsVisible="{Binding SelectedExam, Converter={x:Static ObjectConverters.IsNotNull}}">
    <Button Content="{Binding RetakeButtonText}"
            Classes="secondary"
            Command="{Binding RetakeExamCommand}"
            IsVisible="{Binding CanRetake}"/>
    
    <Button Content="重做练习"
            Classes="secondary"
            Command="{Binding PracticeExamCommand}"
            IsVisible="{Binding CanPractice}"/>
    
    <Button Content="查看历史"
            Command="{Binding ViewExamHistoryCommand}"/>
  </StackPanel>
</StackPanel>
```

#### 3. 考试历史记录显示
```xml
<!-- 考试历史记录 -->
<Border Background="{DynamicResource SystemControlBackgroundChromeMediumLowBrush}"
        CornerRadius="12" Padding="24"
        IsVisible="{Binding ExamAttemptHistory.Count, Converter={x:Static IntConverters.IsGreaterThan}, ConverterParameter=0}">
  <StackPanel Spacing="16">
    <!-- 历史记录标题 -->
    <StackPanel Orientation="Horizontal" Spacing="12">
      <TextBlock Text="📊" FontSize="32"/>
      <StackPanel VerticalAlignment="Center">
        <TextBlock Text="考试历史" FontSize="20" FontWeight="SemiBold"/>
        <TextBlock Text="{Binding ExamAttemptHistory.Count, StringFormat='共 {0} 次考试记录'}"/>
      </StackPanel>
    </StackPanel>
    
    <!-- 历史记录列表 -->
    <ScrollViewer MaxHeight="300">
      <ItemsControl ItemsSource="{Binding ExamAttemptHistory}">
        <ItemsControl.ItemTemplate>
          <DataTemplate>
            <Border Background="{DynamicResource SystemControlBackgroundAltHighBrush}"
                    CornerRadius="8" Padding="16" Margin="0,0,0,8">
              <Grid>
                <Grid.ColumnDefinitions>
                  <ColumnDefinition Width="*"/>
                  <ColumnDefinition Width="Auto"/>
                  <ColumnDefinition Width="Auto"/>
                  <ColumnDefinition Width="Auto"/>
                </Grid.ColumnDefinitions>
                
                <StackPanel Grid.Column="0">
                  <TextBlock Text="{Binding AttemptTypeDisplay}"/>
                  <TextBlock Text="{Binding StartedAt, StringFormat='开始时间: {0:yyyy-MM-dd HH:mm}'}"/>
                </StackPanel>
                
                <StackPanel Grid.Column="1">
                  <TextBlock Text="{Binding StatusDisplay}"/>
                  <TextBlock Text="{Binding ScorePercentageDisplay}"/>
                </StackPanel>
                
                <StackPanel Grid.Column="2">
                  <TextBlock Text="用时"/>
                  <TextBlock Text="{Binding DurationDisplay}"/>
                </StackPanel>
                
                <Border Grid.Column="3" 
                        Background="{DynamicResource SystemAccentColor}"
                        IsVisible="{Binding IsRanked}">
                  <TextBlock Text="计分"/>
                </Border>
              </Grid>
            </Border>
          </DataTemplate>
        </ItemsControl.ItemTemplate>
      </ItemsControl>
    </ScrollViewer>
  </StackPanel>
</Border>
```

### 数据绑定

#### 转换器使用
```xml
<UserControl.Resources>
  <x:Static x:Key="StringConverters.IsNotNullOrEmpty" Member="StringConverters.IsNotNullOrEmpty"/>
  <x:Static x:Key="ObjectConverters.IsNotNull" Member="ObjectConverters.IsNotNull"/>
  <x:Static x:Key="IntConverters.IsGreaterThan" Member="IntConverters.IsGreaterThan"/>
</UserControl.Resources>
```

#### 条件显示
- **考试选择信息**：仅在选择考试后显示
- **重考按钮**：仅在允许重考时显示
- **练习按钮**：仅在允许练习时显示
- **历史记录**：仅在有历史记录时显示
- **计分标识**：仅在参与排名的考试中显示

## 用户交互流程

### 1. 考试选择流程
1. 用户打开考试页面
2. 系统加载可用考试列表
3. 用户从下拉列表选择考试
4. 系统自动检查考试次数限制
5. 显示考试状态和次数统计
6. 根据权限显示相应按钮

### 2. 考试开始流程
1. 用户点击"开始考试"/"重考"/"重做练习"按钮
2. 系统验证考试权限
3. 创建考试尝试记录
4. 启动考试工具栏窗口
5. 更新考试状态为"进行中"

### 3. 历史查看流程
1. 用户选择考试后自动加载历史记录
2. 显示所有考试尝试的详细信息
3. 区分不同类型的考试（首次/重考/练习）
4. 显示是否参与排名

## 响应式设计

### 属性通知
- 使用ReactiveUI的`[Reactive]`特性
- 自动触发PropertyChanged事件
- 支持计算属性的自动更新

### 状态同步
```csharp
// 监听选中考试变化
this.WhenAnyValue(x => x.SelectedExam)
    .Subscribe(async exam => await OnSelectedExamChanged(exam));

// 监听考试状态变化
this.WhenAnyValue(x => x.CurrentExamAttempt)
    .Subscribe(attempt => SyncToolbarStatus(attempt));
```

## 错误处理

### UI错误显示
```xml
<!-- 错误消息显示 -->
<Border Background="{DynamicResource SystemControlBackgroundAccentBrush}"
        IsVisible="{Binding ErrorMessage, Converter={x:Static StringConverters.IsNotNullOrEmpty}}">
  <StackPanel Orientation="Horizontal" Spacing="8">
    <TextBlock Text="⚠️"/>
    <TextBlock Text="{Binding ErrorMessage}" TextWrapping="Wrap"/>
  </StackPanel>
</Border>
```

### 权限验证反馈
- 按钮禁用状态反映权限
- 错误消息提供具体原因
- 状态描述显示当前限制

## 性能优化

### 数据加载
- 异步加载考试列表
- 按需加载考试历史
- 缓存考试配置信息

### UI更新
- 使用虚拟化列表控件
- 限制历史记录显示高度
- 条件渲染减少不必要的UI元素

## 总结

UI集成完成了以下功能：
- ✅ 考试选择和配置显示
- ✅ 考试次数限制信息展示
- ✅ 重考和练习按钮控制
- ✅ 考试历史记录显示
- ✅ 实时状态同步
- ✅ 用户友好的错误提示
- ✅ 响应式数据绑定

整个UI设计遵循Material Design原则，提供了直观、易用的用户体验。
