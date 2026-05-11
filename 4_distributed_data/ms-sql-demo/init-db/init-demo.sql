IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'IsolationDemo')
BEGIN
    CREATE DATABASE IsolationDemo;
    ALTER DATABASE IsolationDemo SET READ_COMMITTED_SNAPSHOT ON;
    ALTER DATABASE IsolationDemo SET ALLOW_SNAPSHOT_ISOLATION ON;
END
GO
USE IsolationDemo;
GO

IF OBJECT_ID('dbo.Accounts', 'U') IS NULL
BEGIN
    CREATE TABLE Accounts (
        AccountID INT PRIMARY KEY,
        Balance INT
    );
    INSERT INTO Accounts (AccountID, Balance) VALUES (1, 100);
END
GO

IF OBJECT_ID('dbo.Orders', 'U') IS NULL
BEGIN
    CREATE TABLE Orders (
        OrderID INT IDENTITY(1,1) PRIMARY KEY,
        Customer VARCHAR(100)
    );
    INSERT INTO Orders (Customer) VALUES ('Alice'), ('Bob'), ('Charlie');
END
GO
