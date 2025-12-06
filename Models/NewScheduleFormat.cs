using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CCLS.Models;

/// <summary>
/// 新课表格式的主要模型类
/// </summary>
public class NewScheduleFormat
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("TimeLayouts")]
    public Dictionary<string, TimeLayout> TimeLayouts { get; set; } = new Dictionary<string, TimeLayout>();

    [JsonPropertyName("ClassPlans")]
    public Dictionary<string, ClassPlan> ClassPlans { get; set; } = new Dictionary<string, ClassPlan>();

    [JsonPropertyName("Subjects")]
    public Dictionary<string, Subject> Subjects { get; set; } = new Dictionary<string, Subject>();

    [JsonPropertyName("ClassPlanGroups")]
    public Dictionary<string, ClassPlanGroup> ClassPlanGroups { get; set; } = new Dictionary<string, ClassPlanGroup>();

    [JsonPropertyName("SelectedClassPlanGroupId")]
    public string SelectedClassPlanGroupId { get; set; } = string.Empty;
}

/// <summary>
/// 时间布局
/// </summary>
public class TimeLayout
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Layouts")]
    public List<TimeLayoutItem> Layouts { get; set; } = new List<TimeLayoutItem>();

    [JsonPropertyName("AttachedObjects")]
    public Dictionary<string, object> AttachedObjects { get; set; } = new Dictionary<string, object>();

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// 时间布局项
/// </summary>
public class TimeLayoutItem
{
    [JsonPropertyName("StartSecond")]
    public DateTime StartSecond { get; set; }

    [JsonPropertyName("EndSecond")]
    public DateTime EndSecond { get; set; }

    [JsonPropertyName("TimeType")]
    public int TimeType { get; set; } // 0: 上课, 1: 休息

    [JsonPropertyName("IsHideDefault")]
    public bool IsHideDefault { get; set; }

    [JsonPropertyName("DefaultClassId")]
    public string DefaultClassId { get; set; } = string.Empty;

    [JsonPropertyName("BreakName")]
    public string BreakName { get; set; } = string.Empty;

    [JsonPropertyName("ActionSet")]
    public object? ActionSet { get; set; }

    [JsonPropertyName("AttachedObjects")]
    public Dictionary<string, object> AttachedObjects { get; set; } = new Dictionary<string, object>();

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// 课表计划
/// </summary>
public class ClassPlan
{
    [JsonPropertyName("TimeLayoutId")]
    public string TimeLayoutId { get; set; } = string.Empty;

    [JsonPropertyName("TimeRule")]
    public TimeRule TimeRule { get; set; } = new TimeRule();

    [JsonPropertyName("Classes")]
    public List<ClassItem> Classes { get; set; } = new List<ClassItem>();

    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("IsOverlay")]
    public bool IsOverlay { get; set; }

    [JsonPropertyName("OverlaySourceId")]
    public string? OverlaySourceId { get; set; }

    [JsonPropertyName("OverlaySetupTime")]
    public DateTime OverlaySetupTime { get; set; }

    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("AssociatedGroup")]
    public string AssociatedGroup { get; set; } = string.Empty;

    [JsonPropertyName("AttachedObjects")]
    public Dictionary<string, object> AttachedObjects { get; set; } = new Dictionary<string, object>();

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// 时间规则
/// </summary>
public class TimeRule
{
    [JsonPropertyName("WeekDay")]
    public int WeekDay { get; set; } // 1-7, 周一到周日

    [JsonPropertyName("WeekCountDiv")]
    public int WeekCountDiv { get; set; }

    [JsonPropertyName("WeekCountDivTotal")]
    public int WeekCountDivTotal { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// 课程项
/// </summary>
public class ClassItem
{
    [JsonPropertyName("SubjectId")]
    public string SubjectId { get; set; } = string.Empty;

    [JsonPropertyName("IsChangedClass")]
    public bool IsChangedClass { get; set; }

    [JsonPropertyName("IsEnabled")]
    public bool IsEnabled { get; set; }

    [JsonPropertyName("AttachedObjects")]
    public Dictionary<string, object> AttachedObjects { get; set; } = new Dictionary<string, object>();

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// 课程信息
/// </summary>
public class Subject
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("Initial")]
    public string Initial { get; set; } = string.Empty;

    [JsonPropertyName("TeacherName")]
    public string TeacherName { get; set; } = string.Empty;

    [JsonPropertyName("IsOutDoor")]
    public bool IsOutDoor { get; set; }

    [JsonPropertyName("AttachedObjects")]
    public Dictionary<string, object> AttachedObjects { get; set; } = new Dictionary<string, object>();

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}

/// <summary>
/// 课表计划组
/// </summary>
public class ClassPlanGroup
{
    [JsonPropertyName("Name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("IsGlobal")]
    public bool IsGlobal { get; set; }

    [JsonPropertyName("IsActive")]
    public bool IsActive { get; set; }
}