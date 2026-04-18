# C_shop_experiment

## Initialization

- 按照以下步驟啟動：
  1. 安裝

     ```
     dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 8.0.0
     dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
     dotnet add package Microsoft.EntityFrameworkCore.Proxies --version 8.0.0
     ```

  2. 重編譯與安裝
     ```
     dotnet restore
     dotnet build
     ```
  3. 資料庫

     ```
     # 建立遷移紀錄
     dotnet ef migrations add InitialCreate

     # 更新資料庫
     dotnet ef database update
     ```
