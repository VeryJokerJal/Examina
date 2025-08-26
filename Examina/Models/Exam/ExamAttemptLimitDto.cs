using System.ComponentModel;

namespace Examina.Models.Exam;

/// <summary>
/// 考试次数限制验证结果DTO
/// </summary>
public class ExamAttemptLimitDto : INotifyPropertyChanged
{
    private int _examId;
    private int _studentId;
    private bool _canStartExam;
    private bool _canRetake;
    private bool _canPractice;
    private int _totalAttempts;
    private int _retakeAttempts;
    private int _practiceAttempts;
    private int _maxRetakeCount;
    private bool _allowRetake;
    private bool _allowPractice;
    private string? _limitReason;
    private ExamAttemptDto? _lastAttempt;

    /// <summary>
    /// 考试ID
    /// </summary>
    public int ExamId
    {
        get => _examId;
        set
        {
            if (_examId != value)
            {
                _examId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 学生ID
    /// </summary>
    public int StudentId
    {
        get => _studentId;
        set
        {
            if (_studentId != value)
            {
                _studentId = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否可以开始考试（首次或重考）
    /// </summary>
    public bool CanStartExam
    {
        get => _canStartExam;
        set
        {
            if (_canStartExam != value)
            {
                _canStartExam = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否可以重考
    /// </summary>
    public bool CanRetake
    {
        get => _canRetake;
        set
        {
            if (_canRetake != value)
            {
                _canRetake = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否可以重做练习
    /// </summary>
    public bool CanPractice
    {
        get => _canPractice;
        set
        {
            if (_canPractice != value)
            {
                _canPractice = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 总尝试次数
    /// </summary>
    public int TotalAttempts
    {
        get => _totalAttempts;
        set
        {
            if (_totalAttempts != value)
            {
                _totalAttempts = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 重考次数
    /// </summary>
    public int RetakeAttempts
    {
        get => _retakeAttempts;
        set
        {
            if (_retakeAttempts != value)
            {
                _retakeAttempts = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 练习次数
    /// </summary>
    public int PracticeAttempts
    {
        get => _practiceAttempts;
        set
        {
            if (_practiceAttempts != value)
            {
                _practiceAttempts = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 最大重考次数
    /// </summary>
    public int MaxRetakeCount
    {
        get => _maxRetakeCount;
        set
        {
            if (_maxRetakeCount != value)
            {
                _maxRetakeCount = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否允许重考
    /// </summary>
    public bool AllowRetake
    {
        get => _allowRetake;
        set
        {
            if (_allowRetake != value)
            {
                _allowRetake = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否允许重做练习
    /// </summary>
    public bool AllowPractice
    {
        get => _allowPractice;
        set
        {
            if (_allowPractice != value)
            {
                _allowPractice = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 限制原因说明
    /// </summary>
    public string? LimitReason
    {
        get => _limitReason;
        set
        {
            if (_limitReason != value)
            {
                _limitReason = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 最后一次考试尝试记录
    /// </summary>
    public ExamAttemptDto? LastAttempt
    {
        get => _lastAttempt;
        set
        {
            if (_lastAttempt != value)
            {
                _lastAttempt = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 剩余重考次数
    /// </summary>
    public int RemainingRetakeCount
    {
        get
        {
            if (!AllowRetake)
                return 0;

            return Math.Max(0, MaxRetakeCount - RetakeAttempts);
        }
    }

    /// <summary>
    /// 是否已完成首次考试
    /// </summary>
    public bool HasCompletedFirstAttempt
    {
        get
        {
            return LastAttempt != null && 
                   LastAttempt.AttemptType == ExamAttemptType.FirstAttempt && 
                   LastAttempt.Status == ExamAttemptStatus.Completed;
        }
    }

    /// <summary>
    /// 考试状态显示文本
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            if (!HasCompletedFirstAttempt)
            {
                return CanStartExam ? "可以开始考试" : "无法开始考试";
            }

            List<string> status = [];

            if (CanRetake)
            {
                status.Add($"可重考 (剩余{RemainingRetakeCount}次)");
            }
            else if (AllowRetake && RemainingRetakeCount == 0)
            {
                status.Add("重考次数已用完");
            }

            if (CanPractice)
            {
                status.Add("可重做练习");
            }
            else if (!AllowPractice)
            {
                status.Add("不允许重做练习");
            }

            return status.Count > 0 ? string.Join(", ", status) : "考试已完成";
        }
    }

    /// <summary>
    /// 次数统计显示文本
    /// </summary>
    public string AttemptCountDisplay
    {
        get
        {
            List<string> counts = [];

            if (AllowRetake)
            {
                counts.Add($"重考: {RetakeAttempts}/{MaxRetakeCount}");
            }

            if (AllowPractice)
            {
                counts.Add($"练习: {PracticeAttempts}次");
            }

            return counts.Count > 0 ? string.Join(", ", counts) : $"总计: {TotalAttempts}次";
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
