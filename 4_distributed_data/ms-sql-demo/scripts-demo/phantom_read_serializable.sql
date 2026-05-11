-- Session A
SET TRANSACTION ISOLATION LEVEL SERIALIZABLE;
BEGIN TRAN;
SELECT COUNT(*) FROM Orders;
-- Session B tries to insert — will be blocked
-- Prevents phantom reads