USE PharmacyDB;
GO


-- 1.3.1 - Системная дата


SELECT CONVERT(CHAR, GETDATE(), 101) AS CurrentDateFormatted;
GO


-- 1.3.2.1 - usp_InsertSupplier


-- Ошибка: пустое имя (NO CHANGES)
EXEC usp_InsertSupplier @Name = '', @Country = 'Ukraine';

-- Успех (CHANGES DATABASE)
EXEC usp_InsertSupplier @Name = 'Task 1.3.2.1 Supplier', @Country = 'Japan', @Notes = 'Test Note';

SELECT * FROM Suppliers WHERE Name = 'Task 1.3.2.1 Supplier';
GO


-- 1.3.2.2 - usp_ProcessDelivery


DECLARE @CatId INT = (SELECT TOP 1 CategoryId FROM Categories);
DECLARE @SupId INT = (SELECT TOP 1 SupplierId FROM Suppliers);
IF NOT EXISTS (SELECT * FROM Medicines WHERE Name = 'Med 1.3.2.2')
    INSERT INTO Medicines (Name, CategoryId, SupplierId, BasePrice, TotalStock) 
    VALUES ('Med 1.3.2.2', @CatId, @SupId, 10.00, 0);



DECLARE @MedId INT = (SELECT MedicineId FROM Medicines WHERE Name = 'Med 1.3.2.2');
EXEC usp_ProcessDelivery @MedicineId = @MedId, @BatchNumber = 'BT-2', @ExpiryDate = '2030-01-01', @Quantity = -5;



DECLARE @MedId INT = (SELECT MedicineId FROM Medicines WHERE Name = 'Med 1.3.2.2');
EXEC usp_ProcessDelivery @MedicineId = @MedId, @BatchNumber = 'BT-1', @ExpiryDate = '2030-01-01', @Quantity = 100;

SELECT MedicineId, Name, TotalStock FROM Medicines WHERE MedicineId = @MedId;
GO


-- 1.3.3.1 - fn_CountExpensiveMedicines

select avg(m.baseprice) from Medicines as m
JOIN Suppliers as s on s.SupplierId = m.SupplierId
WHERE s.Country = 'japan'

SELECT * from Medicines as m 
JOIN Suppliers as s on s.SupplierId = m.SupplierId  
where Country = 'japan';


SELECT dbo.fn_CountExpensiveMedicines('Japan') AS ExpensiveCount;
SELECT dbo.fn_CountExpensiveMedicines('country') AS ExpensiveCount;
GO


-- 1.3.3.2 - fn_GetSupplierStockValue


SELECT * FROM dbo.fn_GetSupplierStockValue();
GO


-- 1.3.4 - tr_LogPriceChanges

UPDATE Medicines SET BasePrice = BasePrice + 10 WHERE Name = 'Med 1.3.2.2';

SELECT * FROM SystemAudit WHERE ActionType = 'PRICE_CHANGE' ORDER BY LogDate DESC;
GO


-- 1.3.5.5 - usp_UpdateSupplierTopSeller

DECLARE @MedId INT = (SELECT MedicineId FROM Medicines WHERE Name = 'Med 1.3.2.2');
INSERT INTO Sales (MedicineId, Quantity, SoldPrice) VALUES (@MedId, 10, 25.00);

select * FROM Sales

DECLARE @SupId INT = (SELECT SupplierId FROM Medicines WHERE MedicineId = @MedId);
EXEC usp_UpdateSupplierTopSeller @SupplierId = @SupId;

---

DECLARE @MedId INT = (SELECT MedicineId FROM Medicines WHERE Name = 'Med 1.3.2.2');
DECLARE @SupId INT = (SELECT SupplierId FROM Medicines WHERE MedicineId = @MedId);
SELECT SupplierId, Name, Notes FROM Suppliers WHERE SupplierId = @SupId;
GO


-- 1.3.5.10 - usp_ArchiveSmallSalesByCategory


DECLARE @CatId INT = (select CategoryId FROM Categories WHERE name = 'Antibiotics');

SELECT COUNT(*) FROM Sales as s
JOIN Medicines as m on m.MedicineId = s.MedicineId
JOIN Categories as c on c.CategoryId = m.CategoryId
WHERE s.quantity <= 4 and c.CategoryId = @CatId


EXEC usp_ArchiveSmallSalesByCategory @CategoryId = @CatId, @k = 4;

SELECT c.name, s.reason, s.quantity FROM SalesArchive as s
JOIN Medicines as m on m.MedicineId = s.MedicineId
JOIN Categories as c on c.CategoryId = m.CategoryId
WHERE Reason = 'Small sale archive' ORDER BY ArchivedAt DESC;
GO


-- 1.3.5.15 - tr_ProtectStockDeduction

UPDATE Inventory SET Location = 'Back Shelf' WHERE BatchNumber = 'BT-1';

SELECT * FROM SystemAudit WHERE Severity = 'SECURITY' ORDER BY LogDate DESC;
GO


-- 1.3.5.18 - tr_UpdateAuditOnDelivery

DECLARE @MedId INT = (SELECT MedicineId FROM Medicines WHERE Name = 'Med 1.3.2.2');
EXEC usp_ProcessDelivery @MedicineId = @MedId, @BatchNumber = 'BT-3', @ExpiryDate = '2030-01-01', @Quantity = 300;

SELECT * FROM SystemAudit WHERE ActionType = 'DELIVERY' ORDER BY LogDate DESC;
GO


-- tr_LogPriceChanges (Medicines)
-- DISABLE TRIGGER tr_LogPriceChanges ON Medicines;
-- ENABLE TRIGGER tr_LogPriceChanges ON Medicines;

-- tr_ProtectStockDeduction (Inventory)
-- DISABLE TRIGGER tr_ProtectStockDeduction ON Inventory;
-- ENABLE TRIGGER tr_ProtectStockDeduction ON Inventory;

-- tr_UpdateAuditOnDelivery (Inventory)
-- DISABLE TRIGGER tr_UpdateAuditOnDelivery ON Inventory;
-- ENABLE TRIGGER tr_UpdateAuditOnDelivery ON Inventory;
