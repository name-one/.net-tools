SQL Migrations v1.0
(c) InoSoft International LLC 2014-2015

Usage: sqlmigrate <command> <command parameters> [param1=value1 [param2=value2 [...]]]

Commands:

 u, update - Performs schema migrations if there are any new ones, and replaces
             views, user-defined functions and stored procedures with the
             current versions.
    Usage: sqlmigrate update <settings> [optional parameters]
    Parameters:
      settings - Path to the migration settings file.

 h, help - Shows help info.