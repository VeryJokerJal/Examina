using ExaminaWebApplication.Models.Admin;
using System.ComponentModel.DataAnnotations;

namespace ExaminaWebApplication.ViewModels;

/// <summary>
/// 管理员配置管理视图模型
/// </summary>
public class AdminConfigurationViewModel : ViewModelBase
{
    private List<SystemConfigurationDto> _allConfigurations = [];
    private Dictionary<string, List<SystemConfigurationDto>> _configurationsByCategory = [];
    private DeviceLimitConfigurationModel _deviceLimitConfiguration = new();
    private string _selectedCategory = string.Empty;
    private bool _isLoading = false;
    private string _searchKeyword = string.Empty;

    /// <summary>
    /// 所有系统配置
    /// </summary>
    public List<SystemConfigurationDto> AllConfigurations
    {
        get => _allConfigurations;
        set => SetProperty(ref _allConfigurations, value);
    }

    /// <summary>
    /// 按分类分组的配置
    /// </summary>
    public Dictionary<string, List<SystemConfigurationDto>> ConfigurationsByCategory
    {
        get => _configurationsByCategory;
        set => SetProperty(ref _configurationsByCategory, value);
    }

    /// <summary>
    /// 设备限制配置
    /// </summary>
    public DeviceLimitConfigurationModel DeviceLimitConfiguration
    {
        get => _deviceLimitConfiguration;
        set => SetProperty(ref _deviceLimitConfiguration, value);
    }

    /// <summary>
    /// 选中的分类
    /// </summary>
    public string SelectedCategory
    {
        get => _selectedCategory;
        set => SetProperty(ref _selectedCategory, value);
    }

    /// <summary>
    /// 是否正在加载
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// 搜索关键词
    /// </summary>
    public string SearchKeyword
    {
        get => _searchKeyword;
        set
        {
            if (SetProperty(ref _searchKeyword, value))
            {
                OnPropertyChanged(nameof(FilteredConfigurations));
            }
        }
    }

    /// <summary>
    /// 过滤后的配置列表
    /// </summary>
    public List<SystemConfigurationDto> FilteredConfigurations
    {
        get
        {
            if (string.IsNullOrEmpty(SearchKeyword))
            {
                return AllConfigurations;
            }

            string keyword = SearchKeyword.ToLowerInvariant();
            return AllConfigurations.Where(c =>
                c.ConfigKey.ToLowerInvariant().Contains(keyword) ||
                (c.Description?.ToLowerInvariant().Contains(keyword) ?? false) ||
                c.Category.ToLowerInvariant().Contains(keyword)
            ).ToList();
        }
    }

    /// <summary>
    /// 可用的配置分类列表
    /// </summary>
    public List<string> AvailableCategories
    {
        get
        {
            return AllConfigurations
                .Select(c => c.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }

    /// <summary>
    /// 设备管理相关配置
    /// </summary>
    public List<SystemConfigurationDto> DeviceManagementConfigurations
    {
        get
        {
            return AllConfigurations
                .Where(c => c.Category == "DeviceManagement")
                .OrderBy(c => c.ConfigKey)
                .ToList();
        }
    }

    /// <summary>
    /// 是否有配置数据
    /// </summary>
    public bool HasConfigurations => AllConfigurations.Count > 0;

    /// <summary>
    /// 是否有搜索结果
    /// </summary>
    public bool HasSearchResults => FilteredConfigurations.Count > 0;

    /// <summary>
    /// 配置统计信息
    /// </summary>
    public ConfigurationStatistics Statistics
    {
        get
        {
            return new ConfigurationStatistics
            {
                TotalConfigurations = AllConfigurations.Count,
                EnabledConfigurations = AllConfigurations.Count(c => c.IsEnabled),
                DisabledConfigurations = AllConfigurations.Count(c => !c.IsEnabled),
                CategoriesCount = AvailableCategories.Count,
                DeviceManagementConfigurationsCount = DeviceManagementConfigurations.Count
            };
        }
    }

    /// <summary>
    /// 清除搜索
    /// </summary>
    public void ClearSearch()
    {
        SearchKeyword = string.Empty;
    }

    /// <summary>
    /// 按分类筛选
    /// </summary>
    /// <param name="category">分类名称</param>
    public void FilterByCategory(string category)
    {
        SelectedCategory = category;
        OnPropertyChanged(nameof(FilteredConfigurations));
    }

    /// <summary>
    /// 清除分类筛选
    /// </summary>
    public void ClearCategoryFilter()
    {
        SelectedCategory = string.Empty;
        OnPropertyChanged(nameof(FilteredConfigurations));
    }

    /// <summary>
    /// 获取指定分类的配置
    /// </summary>
    /// <param name="category">分类名称</param>
    /// <returns>配置列表</returns>
    public List<SystemConfigurationDto> GetConfigurationsByCategory(string category)
    {
        return AllConfigurations
            .Where(c => c.Category == category)
            .OrderBy(c => c.ConfigKey)
            .ToList();
    }

    /// <summary>
    /// 根据键名查找配置
    /// </summary>
    /// <param name="configKey">配置键名</param>
    /// <returns>配置项，如果不存在则返回null</returns>
    public SystemConfigurationDto? FindConfiguration(string configKey)
    {
        return AllConfigurations.FirstOrDefault(c => c.ConfigKey == configKey);
    }

    /// <summary>
    /// 更新配置项
    /// </summary>
    /// <param name="updatedConfiguration">更新后的配置</param>
    public void UpdateConfiguration(SystemConfigurationDto updatedConfiguration)
    {
        int index = AllConfigurations.FindIndex(c => c.ConfigKey == updatedConfiguration.ConfigKey);
        if (index >= 0)
        {
            AllConfigurations[index] = updatedConfiguration;
            OnPropertyChanged(nameof(AllConfigurations));
            OnPropertyChanged(nameof(FilteredConfigurations));
            OnPropertyChanged(nameof(Statistics));

            // 更新分类字典
            RefreshConfigurationsByCategory();
        }
    }

    /// <summary>
    /// 添加新配置项
    /// </summary>
    /// <param name="newConfiguration">新配置</param>
    public void AddConfiguration(SystemConfigurationDto newConfiguration)
    {
        AllConfigurations.Add(newConfiguration);
        OnPropertyChanged(nameof(AllConfigurations));
        OnPropertyChanged(nameof(FilteredConfigurations));
        OnPropertyChanged(nameof(Statistics));
        OnPropertyChanged(nameof(AvailableCategories));

        // 更新分类字典
        RefreshConfigurationsByCategory();
    }

    /// <summary>
    /// 移除配置项
    /// </summary>
    /// <param name="configKey">配置键名</param>
    public void RemoveConfiguration(string configKey)
    {
        int index = AllConfigurations.FindIndex(c => c.ConfigKey == configKey);
        if (index >= 0)
        {
            AllConfigurations.RemoveAt(index);
            OnPropertyChanged(nameof(AllConfigurations));
            OnPropertyChanged(nameof(FilteredConfigurations));
            OnPropertyChanged(nameof(Statistics));
            OnPropertyChanged(nameof(AvailableCategories));

            // 更新分类字典
            RefreshConfigurationsByCategory();
        }
    }

    /// <summary>
    /// 刷新按分类分组的配置
    /// </summary>
    private void RefreshConfigurationsByCategory()
    {
        ConfigurationsByCategory = AllConfigurations
            .GroupBy(c => c.Category)
            .ToDictionary(g => g.Key, g => g.ToList());
    }
}

/// <summary>
/// 配置统计信息
/// </summary>
public class ConfigurationStatistics
{
    /// <summary>
    /// 总配置数量
    /// </summary>
    public int TotalConfigurations { get; set; }

    /// <summary>
    /// 启用的配置数量
    /// </summary>
    public int EnabledConfigurations { get; set; }

    /// <summary>
    /// 禁用的配置数量
    /// </summary>
    public int DisabledConfigurations { get; set; }

    /// <summary>
    /// 分类数量
    /// </summary>
    public int CategoriesCount { get; set; }

    /// <summary>
    /// 设备管理配置数量
    /// </summary>
    public int DeviceManagementConfigurationsCount { get; set; }
}
