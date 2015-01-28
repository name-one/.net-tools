SQL Migrations v1.0.1
(c) InoSoft International LLC 2014-2015

Usage: sqlmigrate <command> <cmd params> [param1=value1 [param2=value2 [...]]]

Commands:

  u, update
    Performs schema migrations if there are any new ones, and replaces views,
    user-defined functions, and stored procedures with their current versions.

    Usage: sqlmigrate u <settings> [optional params]
    Parameters:
      settings - Path to the migration settings file.

  sr, sqlver-migrate-repo
    Converts a Sqlver repository into a set of migrations in a SQL project.

    Usage: sqlmigrate sr <repo-path> <output-dir> [optional params]
    Parameters:
      repo-path  - Path to the Sqlver repository file.
      output-dir - Directory to put the migrations into.
          CAUTION: Existing files in this directory will be deleted.

  sc, sqlver-migrate-copy
    Converts a Sqlver working copy to a SQL Migrations-managed database.

    Usage: sqlmigrate sc <copy-path> <project-path> [optional params]
    Parameters:
      copy-path - Path to the Sqlver working copy that is to be converted.
          CAUTION: This file will be overwritten by a migration settings file.
      project-path - Path to the SQL project that SQL Migrations will be using.

  h, help
    Shows help info.