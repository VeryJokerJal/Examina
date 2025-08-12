using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ExamLab.Models;

namespace ExamLab.Services;

/// <summary>
/// 自动保存服务
/// </summary>
public class AutoSaveService : IDisposable
{
    private static readonly Lazy<AutoSaveService> _instance = new(() => new AutoSaveService());
    public static AutoSaveService Instance => _instance.Value;

    private readonly Timer _autoSaveTimer;
    private readonly object _lockObject = new();
    private bool _isAutoSaveEnabled;
    private int _autoSaveInterval = 300; // 默认5分钟
    private bool _hasUnsavedChanges;
    private ObservableCollection<Exam>? _examsToWatch;
    private bool _disposed;

    /// <summary>
    /// 是否有未保存的更改
    /// </summary>
    public bool HasUnsavedChanges
    {
        get => _hasUnsavedChanges;
        private set
        {
            _hasUnsavedChanges = value;
            UnsavedChangesChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// 未保存更改状态变化事件
    /// </summary>
    public event Action<bool>? UnsavedChangesChanged;

    /// <summary>
    /// 自动保存完成事件
    /// </summary>
    public event Action<bool, string?>? AutoSaveCompleted;

    private AutoSaveService()
    {
        _autoSaveTimer = new Timer(AutoSaveCallback, null, Timeout.Infinite, Timeout.Infinite);
        LoadSettings();
    }

    /// <summary>
    /// 启动自动保存
    /// </summary>
    public void StartAutoSave(ObservableCollection<Exam> exams)
    {
        lock (_lockObject)
        {
            _examsToWatch = exams;

            if (_isAutoSaveEnabled)
            {
                _ = _autoSaveTimer.Change(TimeSpan.FromSeconds(_autoSaveInterval), TimeSpan.FromSeconds(_autoSaveInterval));
            }

            // 监听集合变化
            _examsToWatch.CollectionChanged += OnExamsCollectionChanged;

            // 监听每个试卷的变化
            foreach (Exam exam in _examsToWatch)
            {
                WatchExamChanges(exam);
            }
        }
    }

    /// <summary>
    /// 停止自动保存
    /// </summary>
    public void StopAutoSave()
    {
        lock (_lockObject)
        {
            _ = _autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);

            if (_examsToWatch != null)
            {
                _examsToWatch.CollectionChanged -= OnExamsCollectionChanged;

                foreach (Exam exam in _examsToWatch)
                {
                    UnwatchExamChanges(exam);
                }

                _examsToWatch = null;
            }
        }
    }

    /// <summary>
    /// 设置自动保存配置
    /// </summary>
    public void ConfigureAutoSave(bool enabled, int intervalSeconds)
    {
        lock (_lockObject)
        {
            _isAutoSaveEnabled = enabled;
            _autoSaveInterval = intervalSeconds;

            if (_isAutoSaveEnabled && _examsToWatch != null)
            {
                _ = _autoSaveTimer.Change(TimeSpan.FromSeconds(_autoSaveInterval), TimeSpan.FromSeconds(_autoSaveInterval));
            }
            else
            {
                _ = _autoSaveTimer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }
    }

    /// <summary>
    /// 手动保存
    /// </summary>
    public async Task<bool> SaveNowAsync()
    {
        if (_examsToWatch == null)
        {
            return false;
        }

        try
        {
            await DataStorageService.Instance.SaveExamsAsync(_examsToWatch);
            HasUnsavedChanges = false;
            AutoSaveCompleted?.Invoke(true, null);
            return true;
        }
        catch (Exception ex)
        {
            AutoSaveCompleted?.Invoke(false, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 标记有未保存的更改
    /// </summary>
    public void MarkAsChanged()
    {
        HasUnsavedChanges = true;
    }

    /// <summary>
    /// 标记已保存
    /// </summary>
    public void MarkAsSaved()
    {
        HasUnsavedChanges = false;
    }

    private async void AutoSaveCallback(object? state)
    {
        if (!_hasUnsavedChanges || _examsToWatch == null)
        {
            return;
        }

        try
        {
            await DataStorageService.Instance.SaveExamsAsync(_examsToWatch);
            HasUnsavedChanges = false;
            AutoSaveCompleted?.Invoke(true, null);
        }
        catch (Exception ex)
        {
            AutoSaveCompleted?.Invoke(false, ex.Message);
        }
    }

    private void OnExamsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MarkAsChanged();

        // 监听新添加的试卷
        if (e.NewItems != null)
        {
            foreach (Exam exam in e.NewItems.Cast<Exam>())
            {
                WatchExamChanges(exam);
            }
        }

        // 停止监听移除的试卷
        if (e.OldItems != null)
        {
            foreach (Exam exam in e.OldItems.Cast<Exam>())
            {
                UnwatchExamChanges(exam);
            }
        }
    }

    private void WatchExamChanges(Exam exam)
    {
        // 监听试卷属性变化
        exam.PropertyChanged += OnExamPropertyChanged;

        // 监听模块变化
        exam.Modules.CollectionChanged += OnModulesCollectionChanged;
        foreach (ExamModule module in exam.Modules)
        {
            WatchModuleChanges(module);
        }
    }

    private void UnwatchExamChanges(Exam exam)
    {
        exam.PropertyChanged -= OnExamPropertyChanged;
        exam.Modules.CollectionChanged -= OnModulesCollectionChanged;

        foreach (ExamModule module in exam.Modules)
        {
            UnwatchModuleChanges(module);
        }
    }

    private void WatchModuleChanges(ExamModule module)
    {
        module.PropertyChanged += OnModulePropertyChanged;
        module.Questions.CollectionChanged += OnQuestionsCollectionChanged;

        foreach (Question question in module.Questions)
        {
            WatchQuestionChanges(question);
        }
    }

    private void UnwatchModuleChanges(ExamModule module)
    {
        module.PropertyChanged -= OnModulePropertyChanged;
        module.Questions.CollectionChanged -= OnQuestionsCollectionChanged;

        foreach (Question question in module.Questions)
        {
            UnwatchQuestionChanges(question);
        }
    }

    private void WatchQuestionChanges(Question question)
    {
        question.PropertyChanged += OnQuestionPropertyChanged;
        question.OperationPoints.CollectionChanged += OnOperationPointsCollectionChanged;

        foreach (OperationPoint op in question.OperationPoints)
        {
            WatchOperationPointChanges(op);
        }
    }

    private void UnwatchQuestionChanges(Question question)
    {
        question.PropertyChanged -= OnQuestionPropertyChanged;
        question.OperationPoints.CollectionChanged -= OnOperationPointsCollectionChanged;

        foreach (OperationPoint op in question.OperationPoints)
        {
            UnwatchOperationPointChanges(op);
        }
    }

    private void WatchOperationPointChanges(OperationPoint operationPoint)
    {
        operationPoint.PropertyChanged += OnOperationPointPropertyChanged;
        operationPoint.Parameters.CollectionChanged += OnParametersCollectionChanged;

        foreach (ConfigurationParameter param in operationPoint.Parameters)
        {
            param.PropertyChanged += OnParameterPropertyChanged;
        }
    }

    private void UnwatchOperationPointChanges(OperationPoint operationPoint)
    {
        operationPoint.PropertyChanged -= OnOperationPointPropertyChanged;
        operationPoint.Parameters.CollectionChanged -= OnParametersCollectionChanged;

        foreach (ConfigurationParameter param in operationPoint.Parameters)
        {
            param.PropertyChanged -= OnParameterPropertyChanged;
        }
    }

    private void OnExamPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
    }

    private void OnModulesCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MarkAsChanged();

        if (e.NewItems != null)
        {
            foreach (ExamModule module in e.NewItems.Cast<ExamModule>())
            {
                WatchModuleChanges(module);
            }
        }

        if (e.OldItems != null)
        {
            foreach (ExamModule module in e.OldItems.Cast<ExamModule>())
            {
                UnwatchModuleChanges(module);
            }
        }
    }

    private void OnModulePropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
    }

    private void OnQuestionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MarkAsChanged();

        if (e.NewItems != null)
        {
            foreach (Question question in e.NewItems.Cast<Question>())
            {
                WatchQuestionChanges(question);
            }
        }

        if (e.OldItems != null)
        {
            foreach (Question question in e.OldItems.Cast<Question>())
            {
                UnwatchQuestionChanges(question);
            }
        }
    }

    private void OnQuestionPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
    }

    private void OnOperationPointsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MarkAsChanged();

        if (e.NewItems != null)
        {
            foreach (OperationPoint op in e.NewItems.Cast<OperationPoint>())
            {
                WatchOperationPointChanges(op);
            }
        }

        if (e.OldItems != null)
        {
            foreach (OperationPoint op in e.OldItems.Cast<OperationPoint>())
            {
                UnwatchOperationPointChanges(op);
            }
        }
    }

    private void OnOperationPointPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
    }

    private void OnParametersCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        MarkAsChanged();

        if (e.NewItems != null)
        {
            foreach (ConfigurationParameter param in e.NewItems.Cast<ConfigurationParameter>())
            {
                param.PropertyChanged += OnParameterPropertyChanged;
            }
        }

        if (e.OldItems != null)
        {
            foreach (ConfigurationParameter param in e.OldItems.Cast<ConfigurationParameter>())
            {
                param.PropertyChanged -= OnParameterPropertyChanged;
            }
        }
    }

    private void OnParameterPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        MarkAsChanged();
    }

    private async void LoadSettings()
    {
        try
        {
            AppSettings settings = await DataStorageService.Instance.LoadSettingsAsync();
            _isAutoSaveEnabled = settings.AutoSave;
            _autoSaveInterval = settings.AutoSaveInterval;
        }
        catch
        {
            // 使用默认设置
            _isAutoSaveEnabled = true;
            _autoSaveInterval = 300;
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopAutoSave();
            _autoSaveTimer?.Dispose();
            _disposed = true;
        }
    }
}
