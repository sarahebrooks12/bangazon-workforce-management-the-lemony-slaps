﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using BangazonWorkforce.Models;
using BangazonWorkforce.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Configuration;

namespace BangazonWorkforce.Controllers
{

    public class EmployeesController : Controller
    {
        private readonly IConfiguration _config;

        public EmployeesController(IConfiguration config)
        {
            _config = config;
        }
        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        // GET: EmployeesController
        public ActionResult Index()
        {
            return View();
        }

        // GET: EmployeesController/Details/5
        public ActionResult Details(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                      SELECT e.FirstName AS [First Name], e.LastName AS [Last Name], d.[Name] AS [Department Name],
                       c.Make AS [Computer Make], c.Manufacturer AS [Computer Manufacturer], tP.[Name] AS [Training Program]
                       FROM Employee e
                       LEFT JOIN Department d on e.DepartmentId = d.Id
                       LEFT JOIN ComputerEmployee cE on cE.EmployeeId = e.Id
                       LEFT JOIN Computer c on cE.ComputerId = c.Id
                       LEFT JOIN EmployeeTraining eT on eT.EmployeeId = e.Id
                       LEFT JOIN TrainingProgram tP on eT.TrainingProgramId = tP.Id
                       WHERE e.Id = @id AND cE.UnassignDate is NULL";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Employees employee = null;

                    while (reader.Read())
                    {
                        if (employee == null && reader.IsDBNull(reader.GetOrdinal("Training Program")) && reader.IsDBNull(reader.GetOrdinal("Computer Make")))
                            employee = new Employees
                       
                            {
                                FirstName = reader.GetString(reader.GetOrdinal("First Name")),
                                LastName = reader.GetString(reader.GetOrdinal("Last Name")),
                                department = new Departments
                                {
                                    Name = reader.GetString(reader.GetOrdinal("Department Name")),
                                },
                                computer = new Computers
                                {
                                    Make = reader.GetString(reader.GetOrdinal("Computer Make")),
                                    Manufacturer = reader.GetString(reader.GetOrdinal("Computer Manufacturer"))
                                }
                            };
                        else if (employee == null && !reader.IsDBNull(reader.GetOrdinal("Training Program")) && !reader.IsDBNull(reader.GetOrdinal("Computer Make")))
                            employee = new Employees
                            {
                                FirstName = reader.GetString(reader.GetOrdinal("First Name")),
                                LastName = reader.GetString(reader.GetOrdinal("Last Name")),
                                department = new Departments
                                {
                                    Name = reader.GetString(reader.GetOrdinal("Department Name")),
                                },
                                computer = new Computers
                                {
                                    Make = reader.GetString(reader.GetOrdinal("Computer Make")),
                                    Manufacturer = reader.GetString(reader.GetOrdinal("Computer Manufacturer"))
                                }
                            };
                        TrainingPrograms TrainingProgramList = new TrainingPrograms
                        {
                            Name = reader.GetString(reader.GetOrdinal("Training Program"))
                        };

                        employee.TrainingProgramList.Add(TrainingProgramList);

                    }
                    reader.Close();

                    return View(employee);
                }
            }
        }

        // GET: EmployeesController/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: EmployeesController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: EmployeesController/Edit/5
        public ActionResult Edit(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            e.Id, e.FirstName, e.LastName, e.isSupervisor, e.DepartmentId, c.Make, c.Manufacturer,c.id AS ComputerId
                        FROM Employee e JOIN ComputerEmployee ce ON ce.EmployeeId = e.id JOIN Computer c ON c.id = ce.ComputerId
                        WHERE e.Id = @id AND ce.UnassignDate IS NULL";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    EmployeeEditViewModel viewModel = new EmployeeEditViewModel();

                    if (reader.Read())
                    {
                        viewModel.employee = new Employees
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            isSupervisor = reader.GetBoolean(reader.GetOrdinal("IsSupervisor")),
                            DepartmentId = reader.GetInt32(reader.GetOrdinal("DepartmentId")),
                            computer = new Computers()
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("ComputerId")),
                                Make = reader.GetString(reader.GetOrdinal("Make")),
                                Manufacturer = reader.GetString(reader.GetOrdinal("Manufacturer"))
                            }
                        };

                        // Use the info to build our SelectListItem
                        SelectListItem currentComputerTag = new SelectListItem()
                        {
                            Text = viewModel.employee.computer.Make + ' ' + viewModel.employee.computer.Manufacturer,
                            Value = viewModel.employee.computer.Id.ToString()
                        };
                        viewModel.currentCompId = viewModel.employee.computer.Id;
                        viewModel.computers.Add(currentComputerTag);
                        
                    }
                    reader.Close();

                    // Select all the departments
                    cmd.CommandText = @"SELECT d.Id, d.Name FROM Department d";

                        SqlDataReader Deptreader = cmd.ExecuteReader();
                        // Create a new instance of our view model

                        while (Deptreader.Read())
                        {
                            // Map the raw data to our departments model
                            Departments department = new Departments
                            {
                                Id = Deptreader.GetInt32(Deptreader.GetOrdinal("Id")),
                                Name = Deptreader.GetString(Deptreader.GetOrdinal("Name"))
                            };

                            // Use the info to build our SelectListItem
                            SelectListItem departmentOptionTag = new SelectListItem()
                            {
                                Text = department.Name,
                                Value = department.Id.ToString()
                            };

                            // Add the select list item to our list of dropdown options
                            viewModel.departments.Add(departmentOptionTag);

                        }

                        Deptreader.Close();

                    // Select all the computers
                    cmd.CommandText = @"SELECT DISTINCT ce.ComputerId, c.Make, c.Manufacturer, c.Id FROM Computer c
                        RIGHT JOIN ComputerEmployee ce ON ce.ComputerId = c.id
                        WHERE ce.UnassignDate IS NOT NULL";


//SELECT ce.Id, ce.EmployeeId, ce.ComputerId, ce.AssignDate, ce.UnassignDate, c.Make, c.Manufacturer FROM ComputerEmployee ce JOIN Computer c ON ce.ComputerId = c.id
//                        WHERE ce.UnassignDate IS NOT NULL";

                        SqlDataReader Compreader = cmd.ExecuteReader();
                        // Create a new instance of our view model

                        while (Compreader.Read())
                        {
                            // Map the raw data to our departments model
                            Computers computer = new Computers
                            {
                                Id = Compreader.GetInt32(Compreader.GetOrdinal("Id")),
                                Make = Compreader.GetString(Compreader.GetOrdinal("Make")),
                                Manufacturer = Compreader.GetString(Compreader.GetOrdinal("Manufacturer"))
                            };

                            // Use the info to build our SelectListItem
                            SelectListItem computerOptionTag = new SelectListItem()
                            {
                                Text = computer.Make + computer.Manufacturer,
                                Value = computer.Id.ToString()
                            };

                            // Add the select list item to our list of dropdown options
                            viewModel.computers.Add(computerOptionTag);

                        }
                        

                        Compreader.Close();

                        // If we got something back, send it to the view
                        if (viewModel.employee != null)
                        {
                            // send it all to the view
                            return View(viewModel);
                        }
                        else
                        {

                            // If not, send it to our custom not found page
                            return null;
                        }
                    }
                }
            }
        
  
        // POST: EmployeesController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, EmployeeEditViewModel viewModel)
    {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Employee
                                            SET FirstName = @firstName,
                                                LastName = @lastName,
                                                isSupervisor = @isSupervisor,
                                                DepartmentId = @departmentId
                                            WHERE Id = @id ";
                        cmd.Parameters.Add(new SqlParameter("@firstName", viewModel.employee.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@lastName", viewModel.employee.LastName));
                        cmd.Parameters.Add(new SqlParameter("@isSupervisor", viewModel.employee.isSupervisor));
                        cmd.Parameters.Add(new SqlParameter("@departmentId", viewModel.employee.DepartmentId));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        if (viewModel.employee.computer.Id != viewModel.currentCompId)
                        {
                            cmd.CommandText += @"Update ComputerEmployee
                                                SET UnassignDate = GetDate()
                                                WHERE EmployeeId = @id AND UnassignDate IS NULL
                                                Insert INTO ComputerEmployee (EmployeeId, ComputerId, AssignDate, UnassignDate) VALUES (@id,@computerId,GetDate(), null)";

                            cmd.Parameters.Add(new SqlParameter("@computerId", viewModel.employee.computer.Id));
                        }
                            int rowsAffected = cmd.ExecuteNonQuery();
                            if (rowsAffected > 0)
                            {
                                return RedirectToAction(nameof(Index));
                            }
                            throw new Exception("No rows affected");
                        }
                    }
                }
            
            catch (Exception)
            {
                return View(viewModel);
            }
        }

        // GET: EmployeesController/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: EmployeesController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}
