-- Session A
SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;
BEGIN TRAN;
UPDATE Accounts SET Balance = 200 WHERE AccountID = 1;
-- Do NOT commit yet
-- Keep this transaction open
-- In another session, run the following