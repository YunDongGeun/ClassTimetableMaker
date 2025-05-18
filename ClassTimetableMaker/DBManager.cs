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

                // SQL 삽입 명령 생성
                string sql = @"
                    INSERT INTO timetable_blocks (
                        professor_name, class_name, classroom, grade, course_type, 
                        is_fixed_time, has_additional_restrictions, fixed_time_slot,
                        unavailable_slot1, unavailable_slot2,
                        additional_unavailable_slot1, additional_unavailable_slot2,
                        course_hour1, course_hour2
                    ) VALUES (
                        @ProfessorName, @ClassName, @Classroom, @Grade, @CourseType,
                        @IsFixedTime, @HasAdditionalRestrictions, @FixedTimeSlot,
                        @UnavailableSlot1, @UnavailableSlot2,
                        @AdditionalUnavailableSlot1, @AdditionalUnavailableSlot2,
                        @CourseHour1, @CourseHour2
                    )";

                using var command = new MySqlCommand(sql, connection);

                // 파라미터 추가
                command.Parameters.AddWithValue("@ProfessorName", block.ProfessorName);
                command.Parameters.AddWithValue("@ClassName", block.ClassName);
                command.Parameters.AddWithValue("@Classroom", block.Classroom);
                command.Parameters.AddWithValue("@Grade", block.Grade);
                command.Parameters.AddWithValue("@CourseType", block.CourseType);
                command.Parameters.AddWithValue("@IsFixedTime", block.IsFixedTime);
                command.Parameters.AddWithValue("@HasAdditionalRestrictions", block.HasAdditionalRestrictions);
                command.Parameters.AddWithValue("@FixedTimeSlot", block.FixedTimeSlot ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UnavailableSlot1", block.UnavailableSlot1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@UnavailableSlot2", block.UnavailableSlot2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalUnavailableSlot1", block.AdditionalUnavailableSlot1 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@AdditionalUnavailableSlot2", block.AdditionalUnavailableSlot2 ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@CourseHour1", block.CourseHour1);
                command.Parameters.AddWithValue("@CourseHour2", block.CourseHour2);

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
                        ProfessorName = reader["professor_name"].ToString(),
                        ClassName = reader["class_name"].ToString(),
                        Classroom = reader["classroom"].ToString(),
                        Grade = reader["grade"].ToString(),
                        CourseType = reader["course_type"].ToString(),
                        IsFixedTime = Convert.ToBoolean(reader["is_fixed_time"]),
                        HasAdditionalRestrictions = Convert.ToBoolean(reader["has_additional_restrictions"]),
                        FixedTimeSlot = reader["fixed_time_slot"] == DBNull.Value ? null : reader["fixed_time_slot"].ToString(),
                        UnavailableSlot1 = reader["unavailable_slot1"] == DBNull.Value ? null : reader["unavailable_slot1"].ToString(),
                        UnavailableSlot2 = reader["unavailable_slot2"] == DBNull.Value ? null : reader["unavailable_slot2"].ToString(),
                        AdditionalUnavailableSlot1 = reader["additional_unavailable_slot1"] == DBNull.Value ? null : reader["additional_unavailable_slot1"].ToString(),
                        AdditionalUnavailableSlot2 = reader["additional_unavailable_slot2"] == DBNull.Value ? null : reader["additional_unavailable_slot2"].ToString(),
                        CourseHour1 = Convert.ToInt32(reader["course_hour1"]),
                        CourseHour2 = Convert.ToInt32(reader["course_hour2"])
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
                        ProfessorName = reader["professor_name"].ToString(),
                        ClassName = reader["class_name"].ToString(),
                        Classroom = reader["classroom"].ToString(),
                        Grade = reader["grade"].ToString(),
                        CourseType = reader["course_type"].ToString(),
                        IsFixedTime = Convert.ToBoolean(reader["is_fixed_time"]),
                        HasAdditionalRestrictions = Convert.ToBoolean(reader["has_additional_restrictions"]),
                        FixedTimeSlot = reader["fixed_time_slot"] == DBNull.Value ? null : reader["fixed_time_slot"].ToString(),
                        UnavailableSlot1 = reader["unavailable_slot1"] == DBNull.Value ? null : reader["unavailable_slot1"].ToString(),
                        UnavailableSlot2 = reader["unavailable_slot2"] == DBNull.Value ? null : reader["unavailable_slot2"].ToString(),
                        AdditionalUnavailableSlot1 = reader["additional_unavailable_slot1"] == DBNull.Value ? null : reader["additional_unavailable_slot1"].ToString(),
                        AdditionalUnavailableSlot2 = reader["additional_unavailable_slot2"] == DBNull.Value ? null : reader["additional_unavailable_slot2"].ToString(),
                        CourseHour1 = Convert.ToInt32(reader["course_hour1"]),
                        CourseHour2 = Convert.ToInt32(reader["course_hour2"])
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
    }
}