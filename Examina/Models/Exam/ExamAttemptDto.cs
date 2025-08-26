using System.ComponentModel;

namespace Examina.Models.Exam;

/// <summary>
/// 考试尝试记录DTO
/// </summary>
public class ExamAttemptDto : INotifyPropertyChanged
{
    private int _id;
    private int _examId;
    private int _studentId;
    private int _attemptNumber;
    private ExamAttemptType _attemptType;
    private ExamAttemptStatus _status;
    private DateTime _startedAt;
    private DateTime? _completedAt;
    private decimal? _score;
    private decimal? _maxScore;
    private int? _durationSeconds;
    private string? _notes;
    private bool _isRanked;

    /// <summary>
    /// 考试尝试ID
    /// </summary>
    public int Id
    {
        get => _id;
        set
        {
            if (_id != value)
            {
                _id = value;
                OnPropertyChanged();
            }
        }
    }

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
    /// 尝试次数（从1开始）
    /// </summary>
    public int AttemptNumber
    {
        get => _attemptNumber;
        set
        {
            if (_attemptNumber != value)
            {
                _attemptNumber = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 尝试类型
    /// </summary>
    public ExamAttemptType AttemptType
    {
        get => _attemptType;
        set
        {
            if (_attemptType != value)
            {
                _attemptType = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 考试状态
    /// </summary>
    public ExamAttemptStatus Status
    {
        get => _status;
        set
        {
            if (_status != value)
            {
                _status = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartedAt
    {
        get => _startedAt;
        set
        {
            if (_startedAt != value)
            {
                _startedAt = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 完成时间
    /// </summary>
    public DateTime? CompletedAt
    {
        get => _completedAt;
        set
        {
            if (_completedAt != value)
            {
                _completedAt = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 得分
    /// </summary>
    public decimal? Score
    {
        get => _score;
        set
        {
            if (_score != value)
            {
                _score = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 最大得分
    /// </summary>
    public decimal? MaxScore
    {
        get => _maxScore;
        set
        {
            if (_maxScore != value)
            {
                _maxScore = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 考试用时（秒）
    /// </summary>
    public int? DurationSeconds
    {
        get => _durationSeconds;
        set
        {
            if (_durationSeconds != value)
            {
                _durationSeconds = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 备注信息
    /// </summary>
    public string? Notes
    {
        get => _notes;
        set
        {
            if (_notes != value)
            {
                _notes = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 是否参与排名（重考为true，重做为false）
    /// </summary>
    public bool IsRanked
    {
        get => _isRanked;
        set
        {
            if (_isRanked != value)
            {
                _isRanked = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 考试用时格式化显示
    /// </summary>
    public string DurationDisplay
    {
        get
        {
            if (!DurationSeconds.HasValue)
                return "未完成";

            TimeSpan duration = TimeSpan.FromSeconds(DurationSeconds.Value);
            if (duration.TotalHours >= 1)
                return $"{(int)duration.TotalHours:D2}:{duration.Minutes:D2}:{duration.Seconds:D2}";
            else
                return $"{duration.Minutes:D2}:{duration.Seconds:D2}";
        }
    }

    /// <summary>
    /// 得分百分比显示
    /// </summary>
    public string ScorePercentageDisplay
    {
        get
        {
            if (!Score.HasValue || !MaxScore.HasValue || MaxScore.Value == 0)
                return "未评分";

            decimal percentage = (Score.Value / MaxScore.Value) * 100;
            return $"{percentage:F1}%";
        }
    }

    /// <summary>
    /// 状态显示文本
    /// </summary>
    public string StatusDisplay
    {
        get
        {
            return Status switch
            {
                ExamAttemptStatus.InProgress => "进行中",
                ExamAttemptStatus.Completed => "已完成",
                ExamAttemptStatus.Abandoned => "已放弃",
                ExamAttemptStatus.TimedOut => "超时",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 尝试类型显示文本
    /// </summary>
    public string AttemptTypeDisplay
    {
        get
        {
            return AttemptType switch
            {
                ExamAttemptType.FirstAttempt => "首次考试",
                ExamAttemptType.Retake => "重考",
                ExamAttemptType.Practice => "重做练习",
                _ => "未知"
            };
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 考试尝试类型
/// </summary>
public enum ExamAttemptType
{
    /// <summary>
    /// 首次考试
    /// </summary>
    FirstAttempt = 0,

    /// <summary>
    /// 重考（记录分数和排名）
    /// </summary>
    Retake = 1,

    /// <summary>
    /// 重做练习（不记录分数和排名）
    /// </summary>
    Practice = 2
}

/// <summary>
/// 考试尝试状态
/// </summary>
public enum ExamAttemptStatus
{
    /// <summary>
    /// 进行中
    /// </summary>
    InProgress = 0,

    /// <summary>
    /// 已完成
    /// </summary>
    Completed = 1,

    /// <summary>
    /// 已放弃
    /// </summary>
    Abandoned = 2,

    /// <summary>
    /// 超时
    /// </summary>
    TimedOut = 3
}
