SELECT *
FROM Customers
WHERE CustomerID = @CustomerID
ORDER BY Country, ContactName;