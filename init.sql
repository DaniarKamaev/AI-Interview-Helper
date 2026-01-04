CREATE DATABASE IF NOT EXISTS HelperDb CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
USE HelperDb;

CREATE TABLE IF NOT EXISTS users (
                                     user_id INT AUTO_INCREMENT PRIMARY KEY,
                                     username VARCHAR(50) UNIQUE NOT NULL,
    email VARCHAR(100) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    subscription_tier ENUM('free', 'premium', 'enterprise') DEFAULT 'free',
    subscription_expires_at DATETIME NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_subscription (subscription_tier, subscription_expires_at)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS user_statistics (
                                               stat_id INT AUTO_INCREMENT PRIMARY KEY,
                                               user_id INT UNIQUE NOT NULL,
                                               total_interviews INT DEFAULT 0,
                                               completed_interviews INT DEFAULT 0,
                                               avg_total_score DECIMAL(4,2) NULL,
    best_score DECIMAL(4,2) NULL,
    avg_duration_seconds INT NULL,
    strongest_skill VARCHAR(100) NULL,
    weakest_skill VARCHAR(100) NULL,
    last_interview_date DATETIME NULL,
    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS interviews (
                                          interview_id INT AUTO_INCREMENT PRIMARY KEY,
                                          user_id INT NOT NULL,
                                          job_title VARCHAR(100) NOT NULL COMMENT 'Название позиции (например, "Python-разработчик")',
    job_description TEXT NOT NULL COMMENT 'Описание вакансии от пользователя',
    job_level ENUM('junior', 'middle', 'senior', 'lead') DEFAULT 'middle',
    status ENUM('in_progress', 'completed', 'cancelled', 'timeout') DEFAULT 'in_progress',
    total_score DECIMAL(4,2) NULL COMMENT 'Итоговый балл от 1.00 до 10.00',
    questions_count INT DEFAULT 0,
    correct_answers_count INT DEFAULT 0,
    started_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    completed_at DATETIME NULL,
    duration_seconds INT NULL COMMENT 'Длительность собеседования в секундах',
    ai_feedback_summary TEXT NULL COMMENT 'Общий фидбэк от ИИ',
    recommendations TEXT NULL COMMENT 'Общие рекомендации по изучению',
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    INDEX idx_user_interviews (user_id, started_at),
    INDEX idx_status (status, started_at),
    INDEX idx_completed (completed_at)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS question_categories (
                                                   category_id INT AUTO_INCREMENT PRIMARY KEY,
                                                   name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT NULL,
    parent_category_id INT NULL,
    is_active BOOLEAN DEFAULT TRUE,
    FOREIGN KEY (parent_category_id) REFERENCES question_categories(category_id) ON DELETE SET NULL,
    INDEX idx_parent_category (parent_category_id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS interview_questions (
                                                   question_id INT AUTO_INCREMENT PRIMARY KEY,
                                                   interview_id INT NOT NULL,
                                                   category_id INT NULL,
                                                   question_text TEXT NOT NULL,
                                                   question_type ENUM('technical', 'behavioral', 'theoretical', 'practical') DEFAULT 'technical',
    difficulty_level ENUM('easy', 'medium', 'hard') DEFAULT 'medium',
    turn_number INT NOT NULL COMMENT 'Порядковый номер вопроса в собеседовании',
    asked_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (interview_id) REFERENCES interviews(interview_id) ON DELETE CASCADE,
    FOREIGN KEY (category_id) REFERENCES question_categories(category_id) ON DELETE SET NULL,
    INDEX idx_interview_turn (interview_id, turn_number),
    INDEX idx_category (category_id)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS user_responses (
                                              response_id INT AUTO_INCREMENT PRIMARY KEY,
                                              question_id INT NOT NULL,
                                              interview_id INT NOT NULL,
                                              user_answer TEXT NOT NULL,
                                              response_time_seconds INT NULL COMMENT 'Время на ответ в секундах',
                                              ai_analysis JSON NOT NULL COMMENT 'Детальный анализ ответа от ИИ в формате JSON',
                                              ai_comment TEXT NULL COMMENT 'Текстовый комментарий от ИИ к ответу',
                                              detected_skills JSON NULL COMMENT 'Обнаруженные навыки в ответе',
                                              answered_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                                              FOREIGN KEY (question_id) REFERENCES interview_questions(question_id) ON DELETE CASCADE,
    FOREIGN KEY (interview_id) REFERENCES interviews(interview_id) ON DELETE CASCADE,
    INDEX idx_interview_answers (interview_id, answered_at),
    FULLTEXT INDEX idx_answer_text (user_answer) -- Full-text index для поиска по тексту ответов
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS skill_evaluations (
                                                 evaluation_id INT AUTO_INCREMENT PRIMARY KEY,
                                                 interview_id INT NOT NULL,
                                                 skill_name VARCHAR(100) NOT NULL,
    skill_category VARCHAR(50) NOT NULL COMMENT 'programming, database, soft_skills, etc.',
    score DECIMAL(4,2) NOT NULL,
    confidence_score DECIMAL(4,2) DEFAULT 1.00 COMMENT 'Уверенность ИИ в оценке (0-1)',
    evidence TEXT NULL COMMENT 'Примеры из ответов, подтверждающие оценку',
    improvement_suggestions TEXT NULL COMMENT 'Конкретные рекомендации по улучшению',
    is_final BOOLEAN DEFAULT FALSE COMMENT 'Финальная оценка после всего собеседования',
    evaluated_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (interview_id) REFERENCES interviews(interview_id) ON DELETE CASCADE,
    UNIQUE INDEX idx_unique_skill_eval (interview_id, skill_name, is_final),
    INDEX idx_interview_skills (interview_id, skill_category),
    INDEX idx_skill_score (skill_name, score)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS study_topics (
                                            topic_id INT AUTO_INCREMENT PRIMARY KEY,
                                            interview_id INT NOT NULL,
                                            skill_name VARCHAR(100) NOT NULL,
    topic_name VARCHAR(200) NOT NULL,
    priority ENUM('high', 'medium', 'low') DEFAULT 'medium',
    reason TEXT NULL COMMENT 'Почему эта тема рекомендуется',
    resources JSON NULL COMMENT 'Ссылки на материалы в формате JSON',
    is_completed BOOLEAN DEFAULT FALSE,
    added_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    completed_at DATETIME NULL,
    FOREIGN KEY (interview_id) REFERENCES interviews(interview_id) ON DELETE CASCADE,
    INDEX idx_interview_topics (interview_id, priority),
    INDEX idx_skill_topics (skill_name)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS system_logs (
                                           log_id INT AUTO_INCREMENT PRIMARY KEY,
                                           interview_id INT NULL,
                                           user_id INT NULL,
                                           log_type ENUM('ai_request', 'ai_response', 'error', 'session_event') NOT NULL,
    message TEXT NOT NULL,
    metadata JSON NULL,
    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_logs_interview (interview_id, created_at),
    INDEX idx_logs_user (user_id, created_at),
    INDEX idx_logs_type (log_type, created_at)
    ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO question_categories (name, description) VALUES
('Programming', 'Вопросы по программированию и алгоритмам'),
('Databases', 'Вопросы по базам данных и SQL'),
('System Design', 'Вопросы по проектированию систем'),
('Data Structures', 'Вопросы по структурам данных'),
('Algorithms', 'Вопросы по алгоритмам и сложности'),
('OOP', 'Объектно-ориентированное программирование'),
('Soft Skills', 'Вопросы по коммуникации и поведенческим навыкам'),
('Testing', 'Вопросы по тестированию и QA'),
('DevOps', 'Вопросы по DevOps и инфраструктуре'),
('Security', 'Вопросы по информационной безопасности');