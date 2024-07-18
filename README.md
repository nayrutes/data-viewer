Overview
-
A data viewer and analyzer written in C#, WPF with Entity Framework Core, SQLite, and API connection

Setup
- 
Visual Studio 2022 (Community) solution

Projects:
- The Main Project
  - WPF application with Target Framework .NET 8.0
  - Nuget Packages:
    - Microsoft.EntityFrameworkCore.Sqlite v8.0.7 https://docs.microsoft.com/ef/core/
    - CommunityToolkit.Mvvm 8.2.2 https://github.com/CommunityToolkit/dotnet
- Unit Tests Project
  - WPF application with Target Framework .NET 8.0
  - Nuget Packages:
    - overlet.collectors (preinstalled with Microsoft Unit Test Project Template)
    - Microsoft.NET.Test.Sdk (preinstalled with Microsoft Unit Test Project Template)
    - xunit (https://www.nuget.org/packages/xunit/2.9.0)
    - xunit.runner.visualstudio (https://www.nuget.org/packages/xunit.runner.visualstudio/2.8.2)


Note: currently, the SQLite db is created in the application directory. Make sure the application has write access.

Features
-
- Import CSV data (https://www.briandunning.com/sample-data/), including CSVs with 1M rows
- Auto loading from Db
- Analyze top E-Mail domains
- Analyze Company locations
- Analyze UK Postcodes
  - Fetch geolocations from (https://postcodes.io/)
  - Detect invalid postcodes
  - Cluster people based on geolocations with KMeans Algorithm
- Custom UI Theme with toggle
- Custom Pie-Chart display

Missing Features (TODO)
- Data sanitation during import
- Data edit
  - Data validation
  - Cascading updates (UI and data analysis)
