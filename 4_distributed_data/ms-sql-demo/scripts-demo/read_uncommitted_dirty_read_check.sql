-- Session B
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
SELECT Balance FROM Accounts WHERE AccountID = 1;
-- May read uncommitted value (e.g., 200)