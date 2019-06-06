using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web;
using System.Web.Http.ModelBinding;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OAuth;
using WebApplication2.Models;
using WebApplication2.Providers;
using WebApplication2.Results;
using System.Web.Script.Serialization;
using System.Threading.Tasks;
using Newtonsoft.Json;
using ClassLibrary;

namespace WebApplication2.Controllers
{
    //[Authorize]
    public class DBManageController : ApiController
    {        
        string connectionString = ConfigurationManager.ConnectionStrings["DefaultConnection"].ConnectionString;

        
        [Route("api/DBManage/CreateTable/")]
        [HttpPost]
        public async Task<HttpResponseMessage> CreateTable(FormTableName formTable)
        {
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                //var myNewObject = JsonConvert.DeserializeObject<FormTableName>(model);
                string tableName = $"{formTable.Subject}_{formTable.Course}_{string.Join("", DateTime.Now.ToShortDateString().Split('.'))}_Table";
                string cmd = $"IF NOT EXISTS (SELECT * FROM sysobjects WHERE id = object_id(N'[dbo].[{tableName}]')) CREATE TABLE [{tableName}] ([ID] INTEGER NOT NULL IDENTITY(1, 1),[StudentID] INTEGER NOT NULL PRIMARY KEY,[StudentName] NVARCHAR(256) NOT NULL, [StudentGroup] NVARCHAR(256) NOT NULL, [VisitAt] DATE NOT NULL DEFAULT GETDATE() FOREIGN KEY (StudentID) REFERENCES StudentTable(StudentID))";

                string cmd2 = $"IF NOT EXISTS (SELECT * FROM TableList WHERE TableName = '{tableName}') insert into TableList(TableName,ProfessorID,SubjectName,StudentsCourse,StudentsGroups,UniqueCode) values(N'{tableName}',(select ProfessorID FROM ProfessorTable WHERE EMail = N'{formTable.Email}'),N'{formTable.Subject}',N'{formTable.Course}',N'{string.Join(",", formTable.Groups)}', '{String.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMddHHmmssfff"), Guid.NewGuid())}')";

                await connection.OpenAsync();
                try
                {
                    SqlCommand comm2 = new SqlCommand(cmd2, connection);
                    await comm2.ExecuteNonQueryAsync();

                    SqlCommand comm = new SqlCommand(cmd, connection);
                    await comm.ExecuteNonQueryAsync();

                    return Request.CreateResponse(HttpStatusCode.Created, "Таблица успешно создана.");

                }
                catch (SqlException ex)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "При обработки запроса возникло исключение: " + ex.Message);
                }

               
            }
        }

           
        public async Task<HttpResponseMessage> GetTables(string prepodname)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string cmd = $"select * from TableList where ProfessorID = (select ProfessorID from ProfessorTable where EMail = N'{prepodname}')";
                SqlCommand command = new SqlCommand(cmd, connection);
                List<ProfessorTable> tables = new List<ProfessorTable>();

                try
                {
                    await connection.OpenAsync();
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        ProfessorTable table = new ProfessorTable();
                        table.TableName = reader["TableName"].ToString();
                        table.SubjectName = reader["SubjectName"].ToString();
                        table.StudentsGroups = reader["StudentsGroups"].ToString();
                        table.StudentsCourse = reader["StudentsCourse"].ToString();
                        table.UniqueCode = reader["UniqueCode"].ToString();

                        tables.Add(table);
                    }
                    return Request.CreateResponse(HttpStatusCode.Accepted, tables);
                }
                catch (Exception ex)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "При обработки запроса возникло исключение: " + ex.Message);
                }


            }

        }
        

        public async Task<HttpResponseMessage> GetStudents(string tableName)
        {
            List<Student> students = new List<Student>();
        
            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand($"SELECT StudentName,StudentGroup FROM {tableName} WHERE VisitAt = '{DateTime.Now.ToString("yyyyMMdd")}'", conn);

            try
            {
                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();
        
                while (reader.Read())
                {
                    Student student = new Student();
                    student.Name = reader["StudentName"].ToString();
                    student.Group = reader["StudentGroup"].ToString();
                    students.Add(student);
                }
                return Request.CreateResponse(HttpStatusCode.Accepted, students);

            }
            catch(Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "При выполнении запроса произошла ошибка: " + ex.Message);
                
            }
            finally
            {
                conn.Close();                
            }
            
        }
        public async Task<HttpResponseMessage> GetStudents(string tableName, string visitatdate)
        {
            List<Student> students = new List<Student>();

            SqlConnection conn = new SqlConnection(connectionString);
            SqlCommand cmd = new SqlCommand($"SELECT StudentName,StudentGroup FROM {tableName} WHERE VisitAt = '{visitatdate}'", conn);

            try
            {
                await conn.OpenAsync();
                SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (reader.Read())
                {
                    Student student = new Student();
                    student.Name = reader["StudentName"].ToString();
                    student.Group = reader["StudentGroup"].ToString();
                    students.Add(student);
                }
                return Request.CreateResponse(HttpStatusCode.Accepted, students);

            }
            catch (Exception ex)
            {
                return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "При выполнении запроса произошла ошибка: " + ex.Message);

            }
            finally
            {
                conn.Close();
            }

        }


        public async Task<HttpResponseMessage> PostStudentInfo(MobileUser user)
        {
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                string cmd0 = $"select TableName from TableList where UniqueCode='{user.tablecode}'";
                string tablename = null;
                
                try
                {
                    await connection.OpenAsync();
                    SqlCommand command = new SqlCommand(cmd0, connection);
                    SqlDataReader reader = await command.ExecuteReaderAsync();
                    while (reader.Read())
                    {
                        tablename = reader["TableName"].ToString();
                    }
                    reader.Close();

                    string cmd = string.Format("INSERT INTO [{0}] (StudentID, StudentName, StudentGroup) SELECT StudentID, StudentName, StudentGroup FROM StudentTable  WHERE EMail = '{1}'", tablename,user.useremail);

                    SqlCommand comm = new SqlCommand(cmd, connection);
                    await comm.ExecuteNonQueryAsync();

                    return Request.CreateResponse(HttpStatusCode.Created, "Данные успешно загружены.");

                }
                catch (Exception ex)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, "При создании таблицы произошла ошибка: " + ex.Message);
                }


            }

        }



    }
}

