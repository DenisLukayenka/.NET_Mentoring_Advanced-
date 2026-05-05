-- Session A
SET TRANSACTION ISOLATION LEVEL READ COMMITTED;
BEGIN TRAN;
SELECT Balance FROM Accounts WHERE AccountID = 1;
-- Open another session and run update, then return and re-run the select here
-- Expected: Second SELECT may return a different value