# Total Compensation Calculator – Punch Logic Test

This C# console app calculates employee compensation based on punch-in/punch-out times, job roles, and pay tiers.

## Problem Overview

Employees may:
- Work multiple jobs in a single day
- Accumulate hours across the week
- Earn based on these rules:
  - 0–40 hours: regular time
  - 40–48 hours: overtime (1.5x)
  - Over 48 hours: double time (2x)

## Solution Summary

- Reads input from `clean_data.json`
- Calculates total hours and pay per employee
- Handles job transitions and different pay rates
- Outputs results to `payroll_results.json`

## How to Run

### Requirements
- [.NET 6+ SDK](https://dotnet.microsoft.com/download)

### Steps

```bash
git clone https://github.com/TermianllyChill/InterviewQuestions.git
cd InterviewQuestions/PunchLogicTest
dotnet run
