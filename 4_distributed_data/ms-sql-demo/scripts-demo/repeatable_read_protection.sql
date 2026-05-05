-- Session A
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRAN;
SELECT Balance FROM Accounts WHERE AccountID = 1;
-- In Session B, try to update the same row — it will be blocked
-- Session A keeps a shared lock until COMMIT/ROLLBACK