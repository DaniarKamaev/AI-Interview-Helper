using System;
using System.Collections.Generic;

namespace InterviewHelperAPI;

public partial class QuestionCategory
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public int? ParentCategoryId { get; set; }

    public bool? IsActive { get; set; }

    public virtual ICollection<InterviewQuestion> InterviewQuestions { get; set; } = new List<InterviewQuestion>();

    public virtual ICollection<QuestionCategory> InverseParentCategory { get; set; } = new List<QuestionCategory>();

    public virtual QuestionCategory? ParentCategory { get; set; }
}
