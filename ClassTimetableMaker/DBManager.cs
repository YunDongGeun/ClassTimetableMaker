using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClassTimetableMaker;
using MySqlConnector; // MariaDB 연결을 위한 MySqlConnector 패키지 사용

namespace ClassTimetableMaker
{
    public class DBManager
    {
        // 데이터베이스 연결 문자열
        private readonly string _connectionString;

        public DBManager(string server, int port, string database, string username, string password)
        {
            // 연결 문자열 생성
            _connectionString = $"Server={server};Port={port};Database={database};User ID={username};Password={password};";
        }

        // 데이터베이스 연결 테스트
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // 시간표 블럭 저장
        public async Task<bool> SaveTimeTableBlockAsync(TimeTableBlock block)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                // SQL 삽입 명령 생성 (새로운 필드명과 구조에 맞게 수정)
                string sql = @"
                    INSERT INTO timetable_blocks (
                        professor_name, class_name, classroom, grade,
                        is_fixed_time, fixed_time_slot,
                        unavailable_slot1, unavailable_slot2,
                        additional_unavailable_slot1, additional_unavailable_slot2,
                        lecture_hours_1, lecture_hours_2, section_count
                    ) VALUES (
                        @ProfessorName, @ClassName, @Classroom, @Grade,
                        @IsFixedTime, @FixedTimeSlot,
                        @UnavailableSlot1, @UnavailableSlot2,
                        @AdditionalUnavailableSlot1, @AdditionalUnavailableSlot2,
                        @LectureHours1, @LectureHours2, @SectionCount
                    )";

                using var command = new MySqlCommand(sql, connection);

                // 파라미터 추가 (새로운 필드명에 맞게 수정)
                command.Parameters.AddWithValue("@ProfessorName", block.ProfessorName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClassName", block.ClassName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Classroom", block.Classroom ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Grade", block.Grade ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IsFixedTime", block.IsFixedTime);
                command.Parameters.AddWithValue("@FixedTimeSlot", block.FixedTimeSlot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UnavailableSlot1", block.UnavailableSlot1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UnavailableSlot2", block.UnavailableSlot2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalUnavailableSlot1", block.AdditionalUnavailableSlot1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalUnavailableSlot2", block.AdditionalUnavailableSlot2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LectureHours1", block.LectureHours_1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LectureHours2", block.LectureHours_2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SectionCount", block.SectionCount ?? 1);

                // 명령 실행
                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // 시간표 블럭 조회
        public async Task<List<TimeTableBlock>> GetTimeTableBlocksAsync()
        {
            var blocks = new List<TimeTableBlock>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT * FROM timetable_blocks";
                using var command = new MySqlCommand(sql, connection);
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var block = new TimeTableBlock
                    {
                        Id = Convert.ToInt32(reader["id"]), // ID 필드 추가
                        ProfessorName = reader["professor_name"] == DBNull.Value ? null : reader["professor_name"].ToString(),
                        ClassName = reader["class_name"] == DBNull.Value ? null : reader["class_name"].ToString(),
                        Classroom = reader["classroom"] == DBNull.Value ? null : reader["classroom"].ToString(),
                        Grade = reader["grade"] == DBNull.Value ? null : reader["grade"].ToString(),
                        IsFixedTime = Convert.ToBoolean(reader["is_fixed_time"]),
                        FixedTimeSlot = reader["fixed_time_slot"] == DBNull.Value ? null : reader["fixed_time_slot"].ToString(),
                        UnavailableSlot1 = reader["unavailable_slot1"] == DBNull.Value ? null : reader["unavailable_slot1"].ToString(),
                        UnavailableSlot2 = reader["unavailable_slot2"] == DBNull.Value ? null : reader["unavailable_slot2"].ToString(),
                        AdditionalUnavailableSlot1 = reader["additional_unavailable_slot1"] == DBNull.Value ? null : reader["additional_unavailable_slot1"].ToString(),
                        AdditionalUnavailableSlot2 = reader["additional_unavailable_slot2"] == DBNull.Value ? null : reader["additional_unavailable_slot2"].ToString(),
                        LectureHours_1 = reader["lecture_hours_1"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_1"]),
                        LectureHours_2 = reader["lecture_hours_2"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_2"]),
                        SectionCount = reader["section_count"] == DBNull.Value ? 1 : Convert.ToInt32(reader["section_count"])
                    };

                    blocks.Add(block);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }

            return blocks;
        }

        // 교수명으로 시간표 블럭 조회
        public async Task<List<TimeTableBlock>> GetTimeTableBlocksByProfessorAsync(string professorName)
        {
            var blocks = new List<TimeTableBlock>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT * FROM timetable_blocks WHERE professor_name = @ProfessorName";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@ProfessorName", professorName);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var block = new TimeTableBlock
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        ProfessorName = reader["professor_name"] == DBNull.Value ? null : reader["professor_name"].ToString(),
                        ClassName = reader["class_name"] == DBNull.Value ? null : reader["class_name"].ToString(),
                        Classroom = reader["classroom"] == DBNull.Value ? null : reader["classroom"].ToString(),
                        Grade = reader["grade"] == DBNull.Value ? null : reader["grade"].ToString(),
                        IsFixedTime = Convert.ToBoolean(reader["is_fixed_time"]),
                        FixedTimeSlot = reader["fixed_time_slot"] == DBNull.Value ? null : reader["fixed_time_slot"].ToString(),
                        UnavailableSlot1 = reader["unavailable_slot1"] == DBNull.Value ? null : reader["unavailable_slot1"].ToString(),
                        UnavailableSlot2 = reader["unavailable_slot2"] == DBNull.Value ? null : reader["unavailable_slot2"].ToString(),
                        AdditionalUnavailableSlot1 = reader["additional_unavailable_slot1"] == DBNull.Value ? null : reader["additional_unavailable_slot1"].ToString(),
                        AdditionalUnavailableSlot2 = reader["additional_unavailable_slot2"] == DBNull.Value ? null : reader["additional_unavailable_slot2"].ToString(),
                        LectureHours_1 = reader["lecture_hours_1"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_1"]),
                        LectureHours_2 = reader["lecture_hours_2"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_2"]),
                        SectionCount = reader["section_count"] == DBNull.Value ? 1 : Convert.ToInt32(reader["section_count"])
                    };

                    blocks.Add(block);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }

            return blocks;
        }

        // 학년별 시간표 블럭 조회 (새로 추가)
        public async Task<List<TimeTableBlock>> GetTimeTableBlocksByGradeAsync(string grade)
        {
            var blocks = new List<TimeTableBlock>();

            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT * FROM timetable_blocks WHERE grade = @Grade";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Grade", grade);

                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var block = new TimeTableBlock
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        ProfessorName = reader["professor_name"] == DBNull.Value ? null : reader["professor_name"].ToString(),
                        ClassName = reader["class_name"] == DBNull.Value ? null : reader["class_name"].ToString(),
                        Classroom = reader["classroom"] == DBNull.Value ? null : reader["classroom"].ToString(),
                        Grade = reader["grade"] == DBNull.Value ? null : reader["grade"].ToString(),
                        IsFixedTime = Convert.ToBoolean(reader["is_fixed_time"]),
                        FixedTimeSlot = reader["fixed_time_slot"] == DBNull.Value ? null : reader["fixed_time_slot"].ToString(),
                        UnavailableSlot1 = reader["unavailable_slot1"] == DBNull.Value ? null : reader["unavailable_slot1"].ToString(),
                        UnavailableSlot2 = reader["unavailable_slot2"] == DBNull.Value ? null : reader["unavailable_slot2"].ToString(),
                        AdditionalUnavailableSlot1 = reader["additional_unavailable_slot1"] == DBNull.Value ? null : reader["additional_unavailable_slot1"].ToString(),
                        AdditionalUnavailableSlot2 = reader["additional_unavailable_slot2"] == DBNull.Value ? null : reader["additional_unavailable_slot2"].ToString(),
                        LectureHours_1 = reader["lecture_hours_1"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_1"]),
                        LectureHours_2 = reader["lecture_hours_2"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_2"]),
                        SectionCount = reader["section_count"] == DBNull.Value ? 1 : Convert.ToInt32(reader["section_count"])
                    };

                    blocks.Add(block);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }

            return blocks;
        }

        // 시간표 블럭 업데이트 (새로 추가)
        public async Task<bool> UpdateTimeTableBlockAsync(TimeTableBlock block)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = @"
                    UPDATE timetable_blocks SET
                        professor_name = @ProfessorName,
                        class_name = @ClassName,
                        classroom = @Classroom,
                        grade = @Grade,
                        is_fixed_time = @IsFixedTime,
                        fixed_time_slot = @FixedTimeSlot,
                        unavailable_slot1 = @UnavailableSlot1,
                        unavailable_slot2 = @UnavailableSlot2,
                        additional_unavailable_slot1 = @AdditionalUnavailableSlot1,
                        additional_unavailable_slot2 = @AdditionalUnavailableSlot2,
                        lecture_hours_1 = @LectureHours1,
                        lecture_hours_2 = @LectureHours2,
                        section_count = @SectionCount
                    WHERE id = @Id";

                using var command = new MySqlCommand(sql, connection);

                command.Parameters.AddWithValue("@Id", block.Id);
                command.Parameters.AddWithValue("@ProfessorName", block.ProfessorName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ClassName", block.ClassName ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Classroom", block.Classroom ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@Grade", block.Grade ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@IsFixedTime", block.IsFixedTime);
                command.Parameters.AddWithValue("@FixedTimeSlot", block.FixedTimeSlot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UnavailableSlot1", block.UnavailableSlot1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UnavailableSlot2", block.UnavailableSlot2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalUnavailableSlot1", block.AdditionalUnavailableSlot1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalUnavailableSlot2", block.AdditionalUnavailableSlot2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LectureHours1", block.LectureHours_1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@LectureHours2", block.LectureHours_2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@SectionCount", block.SectionCount ?? 1);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // 시간표 블럭 삭제 (새로 추가)
        public async Task<bool> DeleteTimeTableBlockAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "DELETE FROM timetable_blocks WHERE id = @Id";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                int result = await command.ExecuteNonQueryAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }

        // ID로 특정 시간표 블럭 조회 (새로 추가)
        public async Task<TimeTableBlock> GetTimeTableBlockByIdAsync(int id)
        {
            try
            {
                using var connection = new MySqlConnection(_connectionString);
                await connection.OpenAsync();

                string sql = "SELECT * FROM timetable_blocks WHERE id = @Id";
                using var command = new MySqlCommand(sql, connection);
                command.Parameters.AddWithValue("@Id", id);

                using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    return new TimeTableBlock
                    {
                        Id = Convert.ToInt32(reader["id"]),
                        ProfessorName = reader["professor_name"] == DBNull.Value ? null : reader["professor_name"].ToString(),
                        ClassName = reader["class_name"] == DBNull.Value ? null : reader["class_name"].ToString(),
                        Classroom = reader["classroom"] == DBNull.Value ? null : reader["classroom"].ToString(),
                        Grade = reader["grade"] == DBNull.Value ? null : reader["grade"].ToString(),
                        IsFixedTime = Convert.ToBoolean(reader["is_fixed_time"]),
                        FixedTimeSlot = reader["fixed_time_slot"] == DBNull.Value ? null : reader["fixed_time_slot"].ToString(),
                        UnavailableSlot1 = reader["unavailable_slot1"] == DBNull.Value ? null : reader["unavailable_slot1"].ToString(),
                        UnavailableSlot2 = reader["unavailable_slot2"] == DBNull.Value ? null : reader["unavailable_slot2"].ToString(),
                        AdditionalUnavailableSlot1 = reader["additional_unavailable_slot1"] == DBNull.Value ? null : reader["additional_unavailable_slot1"].ToString(),
                        AdditionalUnavailableSlot2 = reader["additional_unavailable_slot2"] == DBNull.Value ? null : reader["additional_unavailable_slot2"].ToString(),
                        LectureHours_1 = reader["lecture_hours_1"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_1"]),
                        LectureHours_2 = reader["lecture_hours_2"] == DBNull.Value ? null : Convert.ToInt32(reader["lecture_hours_2"]),
                        SectionCount = reader["section_count"] == DBNull.Value ? 1 : Convert.ToInt32(reader["section_count"])
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DB 오류: {ex.Message}");
                throw;
            }
        }
    }
}