-- 데이터베이스 생성
CREATE DATABASE IF NOT EXISTS timetable_helper CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;

-- 데이터베이스 선택
USE timetable_helper;

-- 시간표 블럭 테이블 생성
CREATE TABLE IF NOT EXISTS timetable_blocks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    professor_name VARCHAR(50) NOT NULL,
    classroom VARCHAR(50) NOT NULL,
    grade VARCHAR(20) NOT NULL,
    course_type VARCHAR(20) NOT NULL,
    is_fixed_time BOOLEAN NOT NULL DEFAULT FALSE,
    has_additional_restrictions BOOLEAN NOT NULL DEFAULT FALSE,
    unavailable_slot1 VARCHAR(50),
    unavailable_slot2 VARCHAR(50),
    additional_unavailable_slot1 VARCHAR(50),
    additional_unavailable_slot2 VARCHAR(50),
    course_hour1 INT NOT NULL,
    course_hour2 INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);


-- 시간표 블럭 테이블 생성
CREATE TABLE IF NOT EXISTS timetable_blocks (
    id INT AUTO_INCREMENT PRIMARY KEY,
    professor_name VARCHAR(50) NOT NULL,
    classroom VARCHAR(50) NOT NULL,
    grade VARCHAR(20) NOT NULL,
    course_type VARCHAR(20) NOT NULL,
    is_fixed_time BOOLEAN NOT NULL DEFAULT FALSE,
    has_additional_restrictions BOOLEAN NOT NULL DEFAULT FALSE,
    fixed_time_slot VARCHAR(50),
    unavailable_slot1 VARCHAR(50),
    unavailable_slot2 VARCHAR(50),
    additional_unavailable_slot1 VARCHAR(50),
    additional_unavailable_slot2 VARCHAR(50),
    course_hour1 INT NOT NULL,
    course_hour2 INT NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- 인덱스 추가
CREATE INDEX idx_professor_name ON timetable_blocks(professor_name);
CREATE INDEX idx_grade ON timetable_blocks(grade);
CREATE INDEX idx_course_type ON timetable_blocks(course_type);