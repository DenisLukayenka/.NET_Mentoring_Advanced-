# Transaction Isolation Level Demos – SQL Server (Docker)

This guide contains practical examples for demonstrating transaction isolation levels in Microsoft SQL Server using Docker.

---

## 🔧 Prerequisites

- Docker Desktop or Rancher Desktop (Windows/macOS/Linux)
- SQL Server client: SSMS or Azure Data Studio
- Docker Compose project from this archive
- Run with: `docker-compose build`; `docker-compose up -d`

---

## 📂 Database: `IsolationDemo`

### Tables:
- `Accounts(AccountID, Balance)` – for dirty reads, non-repeatable reads, snapshot
- `Orders(OrderID, Customer)` – for phantom reads

---

## 🧪 Demo Scenarios

### 1. Read Uncommitted – Dirty Read

**File:** `read_uncommitted_dirty_read.sql` + `read_uncommitted_dirty_read_check.sql`  
**Sessions:** A (updating) and B (reading)  
**Behavior:** Session B reads uncommitted data from A.

### 2. Read Committed – Non-repeatable Read

**File:** `read_committed_nonrepeatable_read.sql`  
**Sessions:** A (select twice), B (update and commit)  
**Behavior:** Second SELECT in A may return updated data.

### 3. Repeatable Read – Prevent Non-repeatable Read

**File:** `repeatable_read_protection.sql`  
**Sessions:** A (reading), B (tries to update)  
**Behavior:** B is blocked until A commits – prevents non-repeatable read.

### 4. Repeatable Read – Phantom Read Occurs

**File:** `phantom_read_repeatable_read.sql`  
**Sessions:** A (counts rows), B (inserts new row)  
**Behavior:** A sees a new row on second SELECT – phantom read.

### 5. Serializable – Prevent Phantom Read

**File:** `phantom_read_serializable.sql`  
**Sessions:** A (counts rows), B (tries to insert)  
**Behavior:** B is blocked – phantom read is prevented.

### 6. Snapshot Isolation – Consistent View

**File:** `snapshot_isolation.sql`  
**Sessions:** A (reads twice), B (updates and commits)  
**Behavior:** A sees consistent snapshot from transaction start.

---

## 📘 Usage Tips

- Open **two separate query windows** (Session A and B).
- Run `USE IsolationDemo;` before any script.
- Try with different isolation levels and compare behavior.
- Use `COMMIT` or `ROLLBACK` manually to control transaction boundaries.
- Add `WAITFOR DELAY '00:00:10';` if you want to simulate delays.

---

Happy isolating! 🔒🧠
