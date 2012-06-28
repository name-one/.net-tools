InoSoft International LLC
SQL Version Tool v1.1

Usage: issqlver command param1=value1 param2=value2 ...

Commands:

init - Initializes repository using SQL script for first version.
    Parameters:
    repo - Path to repository description file.
    sql - Initial SQL script filename.

commit - Makes new version in repository.
    Parameters:
    repo - Path to repository description file.
    sql - Incremental SQL script filename.

checkout - Initializes working copy (database state).
    Parameters:
    copy - Path to working copy description file.
    repo - Path to repository description file to get versions from it.
    connection - Connection string to pre-created empty database.

update - Updates working copy if corresponding repository has new versions.
    Parameters:
    copy - Path to working copy description file.
    version - Optional parameter. Specifies version number and working copy 
              will be updated up to it. If not specified, head repository 
              version will be taken.