-- Session A
SET TRANSACTION ISOLATION LEVEL SNAPSHOT;
BEGIN TRAN;
SELECT Balance FROM Accounts WHERE AccountID = 1;
-- Session B updates the same row and commits
-- Session A SELECTs again — sees old value
-- Snapshot provides consistent snapshot view