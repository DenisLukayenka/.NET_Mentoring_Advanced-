-- Session A
SET TRANSACTION ISOLATION LEVEL REPEATABLE READ;
BEGIN TRAN;
SELECT COUNT(*) FROM Orders;
-- Session B inserts a new row into Orders
-- Session A repeats the SELECT and sees a new row => Phantom Read