using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace InterviewHelperAPI;

public partial class HelperDbContext : DbContext
{
    public HelperDbContext()
    {
    }

    public HelperDbContext(DbContextOptions<HelperDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Interview> Interviews { get; set; }

    public virtual DbSet<InterviewQuestion> InterviewQuestions { get; set; }

    public virtual DbSet<QuestionCategory> QuestionCategories { get; set; }

    public virtual DbSet<SkillEvaluation> SkillEvaluations { get; set; }

    public virtual DbSet<StudyTopic> StudyTopics { get; set; }

    public virtual DbSet<SystemLog> SystemLogs { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserResponse> UserResponses { get; set; }

    public virtual DbSet<UserStatistic> UserStatistics { get; set; }

//     protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
// #warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
//         => optionsBuilder.UseMySql("server=localhost;port=3306;database=HelperDb;user=root;password=rootpassword", Microsoft.EntityFrameworkCore.ServerVersion.Parse("8.0.44-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Interview>(entity =>
        {
            entity.HasKey(e => e.InterviewId).HasName("PRIMARY");

            entity.ToTable("interviews");

            entity.HasIndex(e => e.CompletedAt, "idx_completed");

            entity.HasIndex(e => new { e.Status, e.StartedAt }, "idx_status");

            entity.HasIndex(e => new { e.UserId, e.StartedAt }, "idx_user_interviews");

            entity.Property(e => e.InterviewId).HasColumnName("interview_id");
            entity.Property(e => e.AiFeedbackSummary)
                .HasComment("Общий фидбэк от ИИ")
                .HasColumnType("text")
                .HasColumnName("ai_feedback_summary");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.CorrectAnswersCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("correct_answers_count");
            entity.Property(e => e.DurationSeconds)
                .HasComment("Длительность собеседования в секундах")
                .HasColumnName("duration_seconds");
            entity.Property(e => e.JobDescription)
                .HasComment("Описание вакансии от пользователя")
                .HasColumnType("text")
                .HasColumnName("job_description");
            entity.Property(e => e.JobLevel)
                .HasDefaultValueSql("'middle'")
                .HasColumnType("enum('junior','middle','senior','lead')")
                .HasColumnName("job_level");
            entity.Property(e => e.JobTitle)
                .HasMaxLength(100)
                .HasComment("Название позиции (например, \"Python-разработчик\")")
                .HasColumnName("job_title");
            entity.Property(e => e.QuestionsCount)
                .HasDefaultValueSql("'0'")
                .HasColumnName("questions_count");
            entity.Property(e => e.Recommendations)
                .HasComment("Общие рекомендации по изучению")
                .HasColumnType("text")
                .HasColumnName("recommendations");
            entity.Property(e => e.StartedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'in_progress'")
                .HasColumnType("enum('in_progress','completed','cancelled','timeout')")
                .HasColumnName("status");
            entity.Property(e => e.TotalScore)
                .HasPrecision(4, 2)
                .HasComment("Итоговый балл от 1.00 до 10.00")
                .HasColumnName("total_score");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Interviews)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("interviews_ibfk_1");
        });

        modelBuilder.Entity<InterviewQuestion>(entity =>
        {
            entity.HasKey(e => e.QuestionId).HasName("PRIMARY");

            entity.ToTable("interview_questions");

            entity.HasIndex(e => e.CategoryId, "idx_category");

            entity.HasIndex(e => new { e.InterviewId, e.TurnNumber }, "idx_interview_turn");

            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.AskedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("asked_at");
            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.DifficultyLevel)
                .HasDefaultValueSql("'medium'")
                .HasColumnType("enum('easy','medium','hard')")
                .HasColumnName("difficulty_level");
            entity.Property(e => e.InterviewId).HasColumnName("interview_id");
            entity.Property(e => e.QuestionText)
                .HasColumnType("text")
                .HasColumnName("question_text");
            entity.Property(e => e.QuestionType)
                .HasDefaultValueSql("'technical'")
                .HasColumnType("enum('technical','behavioral','theoretical','practical')")
                .HasColumnName("question_type");
            entity.Property(e => e.TurnNumber)
                .HasComment("Порядковый номер вопроса в собеседовании")
                .HasColumnName("turn_number");

            entity.HasOne(d => d.Category).WithMany(p => p.InterviewQuestions)
                .HasForeignKey(d => d.CategoryId)
                .HasConstraintName("interview_questions_ibfk_2");

            entity.HasOne(d => d.Interview).WithMany(p => p.InterviewQuestions)
                .HasForeignKey(d => d.InterviewId)
                .HasConstraintName("interview_questions_ibfk_1");
        });

        modelBuilder.Entity<QuestionCategory>(entity =>
        {
            entity.HasKey(e => e.CategoryId).HasName("PRIMARY");

            entity.ToTable("question_categories");

            entity.HasIndex(e => e.ParentCategoryId, "idx_parent_category");

            entity.HasIndex(e => e.Name, "name").IsUnique();

            entity.Property(e => e.CategoryId).HasColumnName("category_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.IsActive)
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_active");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .HasColumnName("name");
            entity.Property(e => e.ParentCategoryId).HasColumnName("parent_category_id");

            entity.HasOne(d => d.ParentCategory).WithMany(p => p.InverseParentCategory)
                .HasForeignKey(d => d.ParentCategoryId)
                .HasConstraintName("question_categories_ibfk_1");
        });

        modelBuilder.Entity<SkillEvaluation>(entity =>
        {
            entity.HasKey(e => e.EvaluationId).HasName("PRIMARY");

            entity.ToTable("skill_evaluations");

            entity.HasIndex(e => new { e.InterviewId, e.SkillCategory }, "idx_interview_skills");

            entity.HasIndex(e => new { e.SkillName, e.Score }, "idx_skill_score");

            entity.HasIndex(e => new { e.InterviewId, e.SkillName, e.IsFinal }, "idx_unique_skill_eval").IsUnique();

            entity.Property(e => e.EvaluationId).HasColumnName("evaluation_id");
            entity.Property(e => e.ConfidenceScore)
                .HasPrecision(4, 2)
                .HasDefaultValueSql("'1.00'")
                .HasComment("Уверенность ИИ в оценке (0-1)")
                .HasColumnName("confidence_score");
            entity.Property(e => e.EvaluatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("evaluated_at");
            entity.Property(e => e.Evidence)
                .HasComment("Примеры из ответов, подтверждающие оценку")
                .HasColumnType("text")
                .HasColumnName("evidence");
            entity.Property(e => e.ImprovementSuggestions)
                .HasComment("Конкретные рекомендации по улучшению")
                .HasColumnType("text")
                .HasColumnName("improvement_suggestions");
            entity.Property(e => e.InterviewId).HasColumnName("interview_id");
            entity.Property(e => e.IsFinal)
                .HasDefaultValueSql("'0'")
                .HasComment("Финальная оценка после всего собеседования")
                .HasColumnName("is_final");
            entity.Property(e => e.Score)
                .HasPrecision(4, 2)
                .HasColumnName("score");
            entity.Property(e => e.SkillCategory)
                .HasMaxLength(50)
                .HasComment("programming, database, soft_skills, etc.")
                .HasColumnName("skill_category");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .HasColumnName("skill_name");

            entity.HasOne(d => d.Interview).WithMany(p => p.SkillEvaluations)
                .HasForeignKey(d => d.InterviewId)
                .HasConstraintName("skill_evaluations_ibfk_1");
        });

        modelBuilder.Entity<StudyTopic>(entity =>
        {
            entity.HasKey(e => e.TopicId).HasName("PRIMARY");

            entity.ToTable("study_topics");

            entity.HasIndex(e => new { e.InterviewId, e.Priority }, "idx_interview_topics");

            entity.HasIndex(e => e.SkillName, "idx_skill_topics");

            entity.Property(e => e.TopicId).HasColumnName("topic_id");
            entity.Property(e => e.AddedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("added_at");
            entity.Property(e => e.CompletedAt)
                .HasColumnType("datetime")
                .HasColumnName("completed_at");
            entity.Property(e => e.InterviewId).HasColumnName("interview_id");
            entity.Property(e => e.IsCompleted)
                .HasDefaultValueSql("'0'")
                .HasColumnName("is_completed");
            entity.Property(e => e.Priority)
                .HasDefaultValueSql("'medium'")
                .HasColumnType("enum('high','medium','low')")
                .HasColumnName("priority");
            entity.Property(e => e.Reason)
                .HasComment("Почему эта тема рекомендуется")
                .HasColumnType("text")
                .HasColumnName("reason");
            entity.Property(e => e.Resources)
                .HasComment("Ссылки на материалы в формате JSON")
                .HasColumnType("json")
                .HasColumnName("resources");
            entity.Property(e => e.SkillName)
                .HasMaxLength(100)
                .HasColumnName("skill_name");
            entity.Property(e => e.TopicName)
                .HasMaxLength(200)
                .HasColumnName("topic_name");

            entity.HasOne(d => d.Interview).WithMany(p => p.StudyTopics)
                .HasForeignKey(d => d.InterviewId)
                .HasConstraintName("study_topics_ibfk_1");
        });

        modelBuilder.Entity<SystemLog>(entity =>
        {
            entity.HasKey(e => e.LogId).HasName("PRIMARY");

            entity.ToTable("system_logs");

            entity.HasIndex(e => new { e.InterviewId, e.CreatedAt }, "idx_logs_interview");

            entity.HasIndex(e => new { e.LogType, e.CreatedAt }, "idx_logs_type");

            entity.HasIndex(e => new { e.UserId, e.CreatedAt }, "idx_logs_user");

            entity.Property(e => e.LogId).HasColumnName("log_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.InterviewId).HasColumnName("interview_id");
            entity.Property(e => e.LogType)
                .HasColumnType("enum('ai_request','ai_response','error','session_event')")
                .HasColumnName("log_type");
            entity.Property(e => e.Message)
                .HasColumnType("text")
                .HasColumnName("message");
            entity.Property(e => e.Metadata)
                .HasColumnType("json")
                .HasColumnName("metadata");
            entity.Property(e => e.UserId).HasColumnName("user_id");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => new { e.SubscriptionTier, e.SubscriptionExpiresAt }, "idx_subscription");

            entity.HasIndex(e => e.Username, "username").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.SubscriptionExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("subscription_expires_at");
            entity.Property(e => e.SubscriptionTier)
                .HasDefaultValueSql("'free'")
                .HasColumnType("enum('free','premium','enterprise')")
                .HasColumnName("subscription_tier");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .HasColumnName("username");
        });

        modelBuilder.Entity<UserResponse>(entity =>
        {
            entity.HasKey(e => e.ResponseId).HasName("PRIMARY");

            entity.ToTable("user_responses");

            entity.HasIndex(e => e.UserAnswer, "idx_answer_text").HasAnnotation("MySql:FullTextIndex", true);

            entity.HasIndex(e => new { e.InterviewId, e.AnsweredAt }, "idx_interview_answers");

            entity.HasIndex(e => e.QuestionId, "question_id");

            entity.Property(e => e.ResponseId).HasColumnName("response_id");
            entity.Property(e => e.AiAnalysis)
                .HasComment("Детальный анализ ответа от ИИ в формате JSON")
                .HasColumnType("json")
                .HasColumnName("ai_analysis");
            entity.Property(e => e.AiComment)
                .HasComment("Текстовый комментарий от ИИ к ответу")
                .HasColumnType("text")
                .HasColumnName("ai_comment");
            entity.Property(e => e.AnsweredAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("answered_at");
            entity.Property(e => e.DetectedSkills)
                .HasComment("Обнаруженные навыки в ответе")
                .HasColumnType("json")
                .HasColumnName("detected_skills");
            entity.Property(e => e.InterviewId).HasColumnName("interview_id");
            entity.Property(e => e.QuestionId).HasColumnName("question_id");
            entity.Property(e => e.ResponseTimeSeconds)
                .HasComment("Время на ответ в секундах")
                .HasColumnName("response_time_seconds");
            entity.Property(e => e.UserAnswer)
                .HasColumnType("text")
                .HasColumnName("user_answer");

            entity.HasOne(d => d.Interview).WithMany(p => p.UserResponses)
                .HasForeignKey(d => d.InterviewId)
                .HasConstraintName("user_responses_ibfk_2");

            entity.HasOne(d => d.Question).WithMany(p => p.UserResponses)
                .HasForeignKey(d => d.QuestionId)
                .HasConstraintName("user_responses_ibfk_1");
        });

        modelBuilder.Entity<UserStatistic>(entity =>
        {
            entity.HasKey(e => e.StatId).HasName("PRIMARY");

            entity.ToTable("user_statistics");

            entity.HasIndex(e => e.UserId, "user_id").IsUnique();

            entity.Property(e => e.StatId).HasColumnName("stat_id");
            entity.Property(e => e.AvgDurationSeconds).HasColumnName("avg_duration_seconds");
            entity.Property(e => e.AvgTotalScore)
                .HasPrecision(4, 2)
                .HasColumnName("avg_total_score");
            entity.Property(e => e.BestScore)
                .HasPrecision(4, 2)
                .HasColumnName("best_score");
            entity.Property(e => e.CompletedInterviews)
                .HasDefaultValueSql("'0'")
                .HasColumnName("completed_interviews");
            entity.Property(e => e.LastInterviewDate)
                .HasColumnType("datetime")
                .HasColumnName("last_interview_date");
            entity.Property(e => e.StrongestSkill)
                .HasMaxLength(100)
                .HasColumnName("strongest_skill");
            entity.Property(e => e.TotalInterviews)
                .HasDefaultValueSql("'0'")
                .HasColumnName("total_interviews");
            entity.Property(e => e.UpdatedAt)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.WeakestSkill)
                .HasMaxLength(100)
                .HasColumnName("weakest_skill");

            entity.HasOne(d => d.User).WithOne(p => p.UserStatistic)
                .HasForeignKey<UserStatistic>(d => d.UserId)
                .HasConstraintName("user_statistics_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
