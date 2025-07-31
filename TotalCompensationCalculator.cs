using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TotalCompensationCalculator
{
    // Models for the JSON data
    public class JobMeta
    {
        [JsonPropertyName("job")]
        public string Job { get; set; } = string.Empty;
        
        [JsonPropertyName("rate")]
        public decimal Rate { get; set; }
        
        [JsonPropertyName("benefitsRate")]
        public decimal BenefitsRate { get; set; }
    }

    public class TimePunch
    {
        [JsonPropertyName("job")]
        public string Job { get; set; } = string.Empty;
        
        [JsonPropertyName("start")]
        public string Start { get; set; } = string.Empty;
        
        [JsonPropertyName("end")]
        public string End { get; set; } = string.Empty;
    }

    public class EmployeeData
    {
        [JsonPropertyName("employee")]
        public string Employee { get; set; } = string.Empty;
        
        [JsonPropertyName("timePunch")]
        public List<TimePunch> TimePunch { get; set; } = new();
    }

    public class PayrollData
    {
        [JsonPropertyName("jobMeta")]
        public List<JobMeta> JobMeta { get; set; } = new();
        
        [JsonPropertyName("employeeData")]
        public List<EmployeeData> EmployeeData { get; set; } = new();
    }

    public class EmployeeResult
    {
        [JsonPropertyName("employee")]
        public string Employee { get; set; } = string.Empty;
        
        [JsonPropertyName("regular")]
        public string Regular { get; set; } = string.Empty;
        
        [JsonPropertyName("overtime")]
        public string Overtime { get; set; } = string.Empty;
        
        [JsonPropertyName("doubletime")]
        public string Doubletime { get; set; } = string.Empty;
        
        [JsonPropertyName("wageTotal")]
        public string WageTotal { get; set; } = string.Empty;
        
        [JsonPropertyName("benefitTotal")]
        public string BenefitTotal { get; set; } = string.Empty;
    }

    public class PayrollCalculator
    {
        private readonly List<JobMeta> _jobMeta;
        
        public PayrollCalculator(List<JobMeta> jobMeta)
        {
            _jobMeta = jobMeta;
        }
        
        // Tried grouping by job first, but that didn't work for overtime calculations
        // Had to process chronologically to get the right results

        public Dictionary<string, EmployeeResult> CalculatePayroll(List<EmployeeData> employeeData)
        {
            var results = new Dictionary<string, EmployeeResult>();

            foreach (var employee in employeeData)
            {
                var result = CalculateEmployeePayroll(employee);
                results[employee.Employee] = result;
            }

            return results;
        }

        private EmployeeResult CalculateEmployeePayroll(EmployeeData employee)
        {
            // Sort punches by time - this was the key insight!
            var sortedPunches = employee.TimePunch
                .Select(p => new
                {
                    Job = p.Job,
                    StartTime = DateTime.ParseExact(p.Start, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    EndTime = DateTime.ParseExact(p.End, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
                })
                .OrderBy(p => p.StartTime)
                .ToList();

            var totalHours = 0m;
            var wageTotal = 0m;
            var benefitTotal = 0m;

            foreach (var punch in sortedPunches)
            {
                var hours = (decimal)(punch.EndTime - punch.StartTime).TotalHours;
                var job = _jobMeta.FirstOrDefault(j => j.Job == punch.Job);
                if (job == null) continue;

                // Benefits are always the same rate
                benefitTotal += hours * job.BenefitsRate;

                // Now handle the wage calculation with overtime rules
                var hoursRemaining = hours;
                var hoursBeforeOvertime = Math.Max(0, 40m - totalHours);
                var regularHours = Math.Min(hoursRemaining, hoursBeforeOvertime);
                
                if (regularHours > 0)
                {
                    wageTotal += regularHours * job.Rate;
                    hoursRemaining -= regularHours;
                    totalHours += regularHours;
                }

                if (hoursRemaining > 0)
                {
                    var hoursBeforeDoubleTime = Math.Max(0, 48m - totalHours);
                    var overtimeHours = Math.Min(hoursRemaining, hoursBeforeDoubleTime);
                    
                    if (overtimeHours > 0)
                    {
                        wageTotal += overtimeHours * job.Rate * 1.5m;
                        hoursRemaining -= overtimeHours;
                        totalHours += overtimeHours;
                    }

                    if (hoursRemaining > 0)
                    {
                        // Double time for anything over 48 hours
                        wageTotal += hoursRemaining * job.Rate * 2m;
                        totalHours += hoursRemaining;
                    }
                }
            }

            // Calculate the final breakdown for output
            var finalRegularHours = Math.Min(totalHours, 40m);
            var finalOvertimeHours = Math.Max(0, Math.Min(totalHours - 40m, 8m));
            var finalDoubleTimeHours = Math.Max(0, totalHours - 48m);

            return new EmployeeResult
            {
                Employee = employee.Employee,
                Regular = finalRegularHours.ToString("F4"),
                Overtime = finalOvertimeHours.ToString("F4"),
                Doubletime = finalDoubleTimeHours.ToString("F4"),
                WageTotal = wageTotal.ToString("F4"),
                BenefitTotal = benefitTotal.ToString("F4")
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // Read the data file - look in the same directory as the executable
                var jsonContent = File.ReadAllText("clean_data.json");

                // Parse it
                var payrollData = JsonSerializer.Deserialize<PayrollData>(jsonContent);
                
                if (payrollData?.JobMeta == null || payrollData?.EmployeeData == null)
                {
                    throw new Exception("Invalid JSON data - missing required fields");
                }
                
                // Do the calculations
                var calculator = new PayrollCalculator(payrollData.JobMeta);
                var results = calculator.CalculatePayroll(payrollData.EmployeeData);

                // Show results
                var outputJson = JsonSerializer.Serialize(results, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                Console.WriteLine("Total Compensation Results:");
                Console.WriteLine("==========================");
                Console.WriteLine(outputJson);

                // Save to file too
                File.WriteAllText("payroll_results.json", outputJson);
                Console.WriteLine("\nResults saved to payroll_results.json");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
    }
} 