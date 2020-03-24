output "DatabasePassword" {
    value = random_password.DatabasePassword.result
}

output "DatabaseServerName" {
    value = azurerm_sql_server.sqlserver.name
}

output "DatabaseName" {
  value = azurerm_sql_database.devsqldatabase.name
}

output "DatabaseUserName" {
  value = azurerm_sql_server.sqlserver.administrator_login
}