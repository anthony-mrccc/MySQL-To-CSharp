# Note
This is my version of the projet, adapted for fiting to my framework DTOs. So unless you have my frameworks (and you certainly don't) this is useless.
# MySQL-To-CSharp
This tool generates C# classes and markup pages for entire MySQL databases (including tables).

# How to use
Run the program with the following arguments

Parameter | Default value | Description
--- | --- | ---
-i | 127.0.0.1 | (optional) IP address of the MySQL server, will use 127.0.0.1 if not specified
-n | 3306 | (optional) Port number of the MySQL server, will use 3306 if not specified
-u | root | (optional) Username, will use root if not specified
-p | | (optional) Password, will use empty password if not specified
-d | | Database name
-t | | (optional) Table name, will generate entire database if not specified
-g | | (optional) Generate SQL statement output - Activate with -g true
-m | | (optional) Generate markup pages for database and tables which can be used in wikis - Activate with -m true
-r | | (optional) Will use this instead of database name for wiki breadcrump generation
--ns | | namespace
-c | | (optional) Generate ctor
-o | exe_path\db_name | (optional) output path
