#!/usr/bin/env bash
set -e

/opt/mssql/bin/sqlservr &

echo "Waiting for SQL Server to start..."

READY=0
for i in {1..50}; do
  if /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$MSSQL_SA_PASSWORD" -Q "SELECT 1" -C > /dev/null 2>&1; then
    READY=1
    echo "SQL Server is ready!"
    break
  fi
  echo "SQL Server not ready yet... ($i/50)"
  sleep 2
done

if [ $READY -eq 0 ]; then
  echo "SQL Server failed to start within timeout. Exiting."
  exit 1
fi

echo "Running initialization scripts..."

for f in /init-db/*.sql; do
  if [ -f "$f" ]; then
    echo "Running $f"
    /opt/mssql-tools18/bin/sqlcmd -S localhost -U SA -P "$MSSQL_SA_PASSWORD" -C -i "$f"
  fi
done

echo "Initialization complete."

wait
