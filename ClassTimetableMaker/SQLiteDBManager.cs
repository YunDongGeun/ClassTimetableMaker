using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ClassTimetableMaker.Model;

namespace ClassTimetableMaker
{
    public class SQLiteDBManager
    {
        private readonly string _connectionString;
        private readonly string _databasePath;

        public SQLiteDBManager(string databasePath = null)
        {
            // 기본 경로는 애플리케이션 폴더의 timetable.db
            _databasePath = databasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "timetable.db");
            _connectionString = $"Data Source={_databasePath};Version=3;";

            // DB 파일이 없으면 생성
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            if (!File.Exists(_databasePath))
            {
                SQLiteConnection.CreateFile(_databasePath);
                CreateTables();
            }
        }

        private void CreateTables()
        {
            var createTablesScript = @"
                -- 1. 교수 테이블
                CREATE TABLE IF NOT EXISTS professors (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    preferred_time_slots TEXT,
                    unavailable_time_slots TEXT,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- 2. 강의실 테이블 (간단 버전)
                CREATE TABLE IF NOT EXISTS classrooms (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL UNIQUE,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- 3. 교과목 테이블
                CREATE TABLE IF NOT EXISTS subjects (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    name TEXT NOT NULL,
                    grade TEXT NOT NULL,
                    course_type TEXT,
                    lecture_hours_1 INTEGER DEFAULT 1,
                    lecture_hours_2 INTEGER DEFAULT 0,
                    section_count INTEGER DEFAULT 1,
                    is_continuous BOOLEAN DEFAULT FALSE,
                    continuous_hours INTEGER DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    updated_at DATETIME DEFAULT CURRENT_TIMESTAMP
                );

                -- 4. 교과목-교수 매핑 테이블
                CREATE TABLE IF NOT EXISTS subject_professors (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    subject_id INTEGER NOT NULL,
                    professor_id INTEGER NOT NULL,
                    is_primary BOOLEAN DEFAULT TRUE,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (subject_id) REFERENCES subjects(id) ON DELETE CASCADE,
                    FOREIGN KEY (professor_id) REFERENCES professors(id) ON DELETE CASCADE,
                    UNIQUE(subject_id, professor_id)
                );

                -- 5. 시간표 배치 테이블
                CREATE TABLE IF NOT EXISTS timetable_assignments (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    subject_id INTEGER NOT NULL,
                    professor_id INTEGER NOT NULL,
                    classroom_id INTEGER,
                    day_of_week INTEGER NOT NULL,
                    period INTEGER NOT NULL,
                    section_name TEXT DEFAULT 'A',
                    instance_index INTEGER DEFAULT 0,
                    duration INTEGER DEFAULT 1,
                    created_at DATETIME DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (subject_id) REFERENCES subjects(id) ON DELETE CASCADE,
                    FOREIGN KEY (professor_id) REFERENCES professors(id) ON DELETE CASCADE,
                    FOREIGN KEY (classroom_id) REFERENCES classrooms(id) ON DELETE SET NULL,
                    CHECK (day_of_week BETWEEN 1 AND 5),
                    CHECK (period BETWEEN 1 AND 9),
                    CHECK (duration BETWEEN 1 AND 3)
                );

                -- 인덱스 생성
                CREATE INDEX IF NOT EXISTS idx_professors_name ON professors(name);
                CREATE INDEX IF NOT EXISTS idx_subjects_grade ON subjects(grade);
                CREATE INDEX IF NOT EXISTS idx_subjects_name ON subjects(name);
                CREATE INDEX IF NOT EXISTS idx_timetable_day_period ON timetable_assignments(day_of_week, period);
                CREATE INDEX IF NOT EXISTS idx_timetable_professor ON timetable_assignments(professor_id);
                CREATE INDEX IF NOT EXISTS idx_timetable_classroom ON timetable_assignments(classroom_id);

                -- 트리거 생성 (updated_at 자동 업데이트)
                CREATE TRIGGER IF NOT EXISTS update_professors_timestamp 
                    AFTER UPDATE ON professors
                    BEGIN
                        UPDATE professors SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
                    END;

                CREATE TRIGGER IF NOT EXISTS update_classrooms_timestamp 
                    AFTER UPDATE ON classrooms
                    BEGIN
                        UPDATE classrooms SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
                    END;

                CREATE TRIGGER IF NOT EXISTS update_subjects_timestamp 
                    AFTER UPDATE ON subjects
                    BEGIN
                        UPDATE subjects SET updated_at = CURRENT_TIMESTAMP WHERE id = NEW.id;
                    END;
            ";

            using var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            using var command = new SQLiteCommand(createTablesScript, connection);
            command.ExecuteNonQuery();
        }

        // ========================================
        // 교수 관련 메서드
        // ========================================

        public async Task<List<Professor>> GetProfessorsAsync()
        {
            var professors = new List<Professor>();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM professors ORDER BY name";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                professors.Add(new Professor
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    PreferredTimeSlots = reader.IsDBNull("preferred_time_slots") ? null : reader.GetString("preferred_time_slots"),
                    UnavailableTimeSlots = reader.IsDBNull("unavailable_time_slots") ? null : reader.GetString("unavailable_time_slots"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return professors;
        }

        public async Task<Professor> GetProfessorByIdAsync(int id)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM professors WHERE id = @id";
            using var command = new SQLiteCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Professor
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    PreferredTimeSlots = reader.IsDBNull("preferred_time_slots") ? null : reader.GetString("preferred_time_slots"),
                    UnavailableTimeSlots = reader.IsDBNull("unavailable_time_slots") ? null : reader.GetString("unavailable_time_slots"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                };
            }

            return null;
        }

        public async Task<bool> SaveProfessorAsync(Professor professor)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO professors (name, preferred_time_slots, unavailable_time_slots)
                    VALUES (@name, @preferredTimeSlots, @unavailableTimeSlots)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@name", professor.Name);
                command.Parameters.AddWithValue("@preferredTimeSlots", professor.PreferredTimeSlots ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@unavailableTimeSlots", professor.UnavailableTimeSlots ?? (object)DBNull.Value);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 강의실 관련 메서드
        // ========================================

        public async Task<List<Classroom>> GetClassroomsAsync()
        {
            var classrooms = new List<Classroom>();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "SELECT * FROM classrooms ORDER BY name";
            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                classrooms.Add(new Classroom
                {
                    Id = reader.GetInt32("id"),
                    Name = reader.GetString("name"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    UpdatedAt = reader.GetDateTime("updated_at")
                });
            }

            return classrooms;
        }

        public async Task<bool> SaveClassroomAsync(Classroom classroom)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO classrooms (name)
                    VALUES (@name)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@name", classroom.Name);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 교과목 관련 메서드
        // ========================================

        public async Task<List<Subject>> GetSubjectsAsync()
        {
            var subjects = new List<Subject>();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT s.*, sp.professor_id, sp.is_primary, p.name as professor_name
                FROM subjects s
                LEFT JOIN subject_professors sp ON s.id = sp.subject_id
                LEFT JOIN professors p ON sp.professor_id = p.id
                ORDER BY s.grade, s.name, sp.is_primary DESC";

            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            var subjectDict = new Dictionary<int, Subject>();

            while (await reader.ReadAsync())
            {
                int subjectId = reader.GetInt32("id");

                if (!subjectDict.ContainsKey(subjectId))
                {
                    subjectDict[subjectId] = new Subject
                    {
                        Id = subjectId,
                        Name = reader.GetString("name"),
                        Grade = reader.GetString("grade"),
                        CourseType = reader.IsDBNull("course_type") ? null : reader.GetString("course_type"),
                        LectureHours1 = reader.GetInt32("lecture_hours_1"),
                        LectureHours2 = reader.GetInt32("lecture_hours_2"),
                        SectionCount = reader.GetInt32("section_count"),
                        IsContinuous = reader.GetBoolean("is_continuous"),
                        ContinuousHours = reader.GetInt32("continuous_hours"),
                        CreatedAt = reader.GetDateTime("created_at"),
                        UpdatedAt = reader.GetDateTime("updated_at"),
                        SubjectProfessors = new List<SubjectProfessor>(),
                        Professors = new List<Professor>()
                    };
                }

                // 교수 정보 추가
                if (!reader.IsDBNull("professor_id"))
                {
                    var professor = new Professor
                    {
                        Id = reader.GetInt32("professor_id"),
                        Name = reader.GetString("professor_name")
                    };

                    var subjectProfessor = new SubjectProfessor
                    {
                        SubjectId = subjectId,
                        ProfessorId = professor.Id,
                        IsPrimary = reader.GetBoolean("is_primary"),
                        Professor = professor
                    };

                    subjectDict[subjectId].SubjectProfessors.Add(subjectProfessor);
                    subjectDict[subjectId].Professors.Add(professor);
                }
            }

            return new List<Subject>(subjectDict.Values);
        }

        public async Task<bool> SaveSubjectAsync(Subject subject, List<int> professorIds)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. 교과목 저장
                string subjectSql = @"
                    INSERT INTO subjects (name, grade, course_type, lecture_hours_1, lecture_hours_2, 
                                        section_count, is_continuous, continuous_hours)
                    VALUES (@name, @grade, @courseType, @lectureHours1, @lectureHours2, 
                           @sectionCount, @isContinuous, @continuousHours)";

                using var subjectCommand = new SQLiteCommand(subjectSql, connection, transaction);
                subjectCommand.Parameters.AddWithValue("@name", subject.Name);
                subjectCommand.Parameters.AddWithValue("@grade", subject.Grade);
                subjectCommand.Parameters.AddWithValue("@courseType", subject.CourseType ?? (object)DBNull.Value);
                subjectCommand.Parameters.AddWithValue("@lectureHours1", subject.LectureHours1);
                subjectCommand.Parameters.AddWithValue("@lectureHours2", subject.LectureHours2);
                subjectCommand.Parameters.AddWithValue("@sectionCount", subject.SectionCount);
                subjectCommand.Parameters.AddWithValue("@isContinuous", subject.IsContinuous);
                subjectCommand.Parameters.AddWithValue("@continuousHours", subject.ContinuousHours);

                await subjectCommand.ExecuteNonQueryAsync();

                // 2. 생성된 교과목 ID 가져오기
                string getIdSql = "SELECT last_insert_rowid()";
                using var getIdCommand = new SQLiteCommand(getIdSql, connection, transaction);
                int subjectId = Convert.ToInt32(await getIdCommand.ExecuteScalarAsync());

                // 3. 교수-교과목 매핑 저장
                if (professorIds != null && professorIds.Count > 0)
                {
                    string mappingSql = @"
                        INSERT INTO subject_professors (subject_id, professor_id, is_primary)
                        VALUES (@subjectId, @professorId, @isPrimary)";

                    for (int i = 0; i < professorIds.Count; i++)
                    {
                        using var mappingCommand = new SQLiteCommand(mappingSql, connection, transaction);
                        mappingCommand.Parameters.AddWithValue("@subjectId", subjectId);
                        mappingCommand.Parameters.AddWithValue("@professorId", professorIds[i]);
                        mappingCommand.Parameters.AddWithValue("@isPrimary", i == 0); // 첫 번째 교수가 주담당

                        await mappingCommand.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 시간표 배치 관련 메서드
        // ========================================

        public async Task<List<TimetableAssignment>> GetTimetableAssignmentsAsync()
        {
            var assignments = new List<TimetableAssignment>();

            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();

            string sql = @"
                SELECT ta.*, s.name as subject_name, s.grade, p.name as professor_name, c.name as classroom_name
                FROM timetable_assignments ta
                LEFT JOIN subjects s ON ta.subject_id = s.id
                LEFT JOIN professors p ON ta.professor_id = p.id
                LEFT JOIN classrooms c ON ta.classroom_id = c.id
                ORDER BY ta.day_of_week, ta.period";

            using var command = new SQLiteCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                assignments.Add(new TimetableAssignment
                {
                    Id = reader.GetInt32("id"),
                    SubjectId = reader.GetInt32("subject_id"),
                    ProfessorId = reader.GetInt32("professor_id"),
                    ClassroomId = reader.IsDBNull("classroom_id") ? null : reader.GetInt32("classroom_id"),
                    DayOfWeek = reader.GetInt32("day_of_week"),
                    Period = reader.GetInt32("period"),
                    SectionName = reader.GetString("section_name"),
                    InstanceIndex = reader.GetInt32("instance_index"),
                    Duration = reader.GetInt32("duration"),
                    CreatedAt = reader.GetDateTime("created_at"),
                    Subject = new Subject { Name = reader.GetString("subject_name"), Grade = reader.GetString("grade") },
                    Professor = new Professor { Name = reader.GetString("professor_name") },
                    Classroom = reader.IsDBNull("classroom_name") ? null : new Classroom { Name = reader.GetString("classroom_name") }
                });
            }

            return assignments;
        }

        public async Task<bool> SaveTimetableAssignmentAsync(TimetableAssignment assignment)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
                    INSERT INTO timetable_assignments 
                    (subject_id, professor_id, classroom_id, day_of_week, period, section_name, instance_index, duration)
                    VALUES (@subjectId, @professorId, @classroomId, @dayOfWeek, @period, @sectionName, @instanceIndex, @duration)";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@subjectId", assignment.SubjectId);
                command.Parameters.AddWithValue("@professorId", assignment.ProfessorId);
                command.Parameters.AddWithValue("@classroomId", assignment.ClassroomId ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@dayOfWeek", assignment.DayOfWeek);
                command.Parameters.AddWithValue("@period", assignment.Period);
                command.Parameters.AddWithValue("@sectionName", assignment.SectionName);
                command.Parameters.AddWithValue("@instanceIndex", assignment.InstanceIndex);
                command.Parameters.AddWithValue("@duration", assignment.Duration);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 레거시 호환 메서드 (기존 UI와 호환)
        // ========================================

        public async Task<List<TimeTableBlock>> GetTimeTableBlocksAsync()
        {
            var subjects = await GetSubjectsAsync();
            var blocks = new List<TimeTableBlock>();

            foreach (var subject in subjects)
            {
                var primaryProfessor = subject.GetPrimaryProfessor();

                var block = new TimeTableBlock
                {
                    Id = subject.Id,
                    ClassName = subject.Name,
                    ProfessorName = primaryProfessor?.Name,
                    Grade = subject.Grade,
                    LectureHours_1 = subject.LectureHours1,
                    LectureHours_2 = subject.LectureHours2,
                    SectionCount = subject.SectionCount,
                    Duration = subject.IsContinuous ? subject.ContinuousHours : Math.Max(subject.LectureHours1, subject.LectureHours2),
                    DisplayName = subject.Name
                };

                blocks.Add(block);
            }

            return blocks;
        }

        public async Task<List<TimeTableBlock>> GetTimeTableBlocksByProfessorAsync(string professorName)
        {
            var allBlocks = await GetTimeTableBlocksAsync();
            return allBlocks.FindAll(b => b.ProfessorName != null &&
                                         b.ProfessorName.Contains(professorName, StringComparison.OrdinalIgnoreCase));
        }

        // ========================================
        // DB 연결 테스트
        // ========================================

        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // ========================================
        // 데이터 삭제 메서드들
        // ========================================

        public async Task<bool> DeleteTimetableAssignmentAsync(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "DELETE FROM timetable_assignments WHERE id = @id";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsProfessorNameExistsAsync(string name)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT COUNT(*) FROM professors WHERE name = @name";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@name", name);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsClassroomNameExistsAsync(string name)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT COUNT(*) FROM classrooms WHERE name = @name";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@name", name);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> IsSubjectNameExistsAsync(string name)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT COUNT(*) FROM subjects WHERE name = @name";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@name", name);

                var result = await command.ExecuteScalarAsync();
                return Convert.ToInt32(result) > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> DeleteProfessorAsync(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                // 관련된 데이터가 있는지 먼저 확인
                string checkSql = @"
            SELECT COUNT(*) FROM subject_professors WHERE professor_id = @id
            UNION ALL
            SELECT COUNT(*) FROM timetable_assignments WHERE professor_id = @id";

                using var checkCommand = new SQLiteCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@id", id);

                using var reader = await checkCommand.ExecuteReaderAsync();
                int relatedCount = 0;
                while (await reader.ReadAsync())
                {
                    relatedCount += reader.GetInt32(0);
                }
                reader.Close();

                if (relatedCount > 0)
                {
                    throw new InvalidOperationException($"이 교수는 {relatedCount}개의 관련 데이터가 있어 삭제할 수 없습니다.");
                }

                // 교수 삭제
                string deleteSql = "DELETE FROM professors WHERE id = @id";
                using var deleteCommand = new SQLiteCommand(deleteSql, connection);
                deleteCommand.Parameters.AddWithValue("@id", id);

                int result = await deleteCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 강의실 삭제 메서드
        // ========================================

        public async Task<bool> DeleteClassroomAsync(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                // 관련된 시간표 배치가 있는지 확인
                string checkSql = "SELECT COUNT(*) FROM timetable_assignments WHERE classroom_id = @id";
                using var checkCommand = new SQLiteCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@id", id);

                int relatedCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (relatedCount > 0)
                {
                    throw new InvalidOperationException($"이 강의실은 {relatedCount}개의 시간표에서 사용 중이어서 삭제할 수 없습니다.");
                }

                // 강의실 삭제
                string deleteSql = "DELETE FROM classrooms WHERE id = @id";
                using var deleteCommand = new SQLiteCommand(deleteSql, connection);
                deleteCommand.Parameters.AddWithValue("@id", id);

                int result = await deleteCommand.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 교과목 삭제 메서드 (이미 있지만 개선)
        // ========================================

        public async Task<bool> DeleteSubjectAsync(int id)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                // 관련된 시간표 배치가 있는지 확인
                string checkSql = "SELECT COUNT(*) FROM timetable_assignments WHERE subject_id = @id";
                using var checkCommand = new SQLiteCommand(checkSql, connection);
                checkCommand.Parameters.AddWithValue("@id", id);

                int relatedCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                if (relatedCount > 0)
                {
                    throw new InvalidOperationException($"이 교과목은 {relatedCount}개의 시간표에 배치되어 있어서 삭제할 수 없습니다.");
                }

                // CASCADE DELETE가 설정되어 있어서 관련 데이터들이 자동으로 삭제됨
                string sql = "DELETE FROM subjects WHERE id = @id";
                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", id);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 교수 정보 업데이트 메서드
        // ========================================

        public async Task<bool> UpdateProfessorAsync(Professor professor)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
            UPDATE professors 
            SET name = @name, 
                preferred_time_slots = @preferredTimeSlots, 
                unavailable_time_slots = @unavailableTimeSlots,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", professor.Id);
                command.Parameters.AddWithValue("@name", professor.Name);
                command.Parameters.AddWithValue("@preferredTimeSlots", professor.PreferredTimeSlots ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@unavailableTimeSlots", professor.UnavailableTimeSlots ?? (object)DBNull.Value);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 강의실 정보 업데이트 메서드
        // ========================================

        public async Task<bool> UpdateClassroomAsync(Classroom classroom)
        {
            try
            {
                using var connection = new SQLiteConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
            UPDATE classrooms 
            SET name = @name,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id";

                using var command = new SQLiteCommand(sql, connection);
                command.Parameters.AddWithValue("@id", classroom.Id);
                command.Parameters.AddWithValue("@name", classroom.Name);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ========================================
        // 교과목 정보 업데이트 메서드
        // ========================================

        public async Task<bool> UpdateSubjectAsync(Subject subject, List<int> professorIds)
        {
            using var connection = new SQLiteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. 교과목 정보 업데이트
                string subjectSql = @"
            UPDATE subjects 
            SET name = @name, 
                grade = @grade, 
                course_type = @courseType, 
                lecture_hours_1 = @lectureHours1, 
                lecture_hours_2 = @lectureHours2,
                section_count = @sectionCount, 
                is_continuous = @isContinuous, 
                continuous_hours = @continuousHours,
                updated_at = CURRENT_TIMESTAMP
            WHERE id = @id";

                using var subjectCommand = new SQLiteCommand(subjectSql, connection, transaction);
                subjectCommand.Parameters.AddWithValue("@id", subject.Id);
                subjectCommand.Parameters.AddWithValue("@name", subject.Name);
                subjectCommand.Parameters.AddWithValue("@grade", subject.Grade);
                subjectCommand.Parameters.AddWithValue("@courseType", subject.CourseType ?? (object)DBNull.Value);
                subjectCommand.Parameters.AddWithValue("@lectureHours1", subject.LectureHours1);
                subjectCommand.Parameters.AddWithValue("@lectureHours2", subject.LectureHours2);
                subjectCommand.Parameters.AddWithValue("@sectionCount", subject.SectionCount);
                subjectCommand.Parameters.AddWithValue("@isContinuous", subject.IsContinuous);
                subjectCommand.Parameters.AddWithValue("@continuousHours", subject.ContinuousHours);

                await subjectCommand.ExecuteNonQueryAsync();

                // 2. 기존 교수-교과목 매핑 삭제
                string deleteMappingSql = "DELETE FROM subject_professors WHERE subject_id = @subjectId";
                using var deleteMappingCommand = new SQLiteCommand(deleteMappingSql, connection, transaction);
                deleteMappingCommand.Parameters.AddWithValue("@subjectId", subject.Id);
                await deleteMappingCommand.ExecuteNonQueryAsync();

                // 3. 새로운 교수-교과목 매핑 추가
                if (professorIds != null && professorIds.Count > 0)
                {
                    string insertMappingSql = @"
                INSERT INTO subject_professors (subject_id, professor_id, is_primary)
                VALUES (@subjectId, @professorId, @isPrimary)";

                    for (int i = 0; i < professorIds.Count; i++)
                    {
                        using var insertMappingCommand = new SQLiteCommand(insertMappingSql, connection, transaction);
                        insertMappingCommand.Parameters.AddWithValue("@subjectId", subject.Id);
                        insertMappingCommand.Parameters.AddWithValue("@professorId", professorIds[i]);
                        insertMappingCommand.Parameters.AddWithValue("@isPrimary", i == 0); // 첫 번째 교수가 주담당

                        await insertMappingCommand.ExecuteNonQueryAsync();
                    }
                }

                transaction.Commit();
                return true;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }
    }
}