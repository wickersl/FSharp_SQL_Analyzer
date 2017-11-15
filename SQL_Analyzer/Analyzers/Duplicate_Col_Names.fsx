#r @"..\..\packages\FSharp.Data.SqlClient.1.8.2\lib\net40\FSharp.Data.SqlClient.dll"

open FSharp.Data
open FSharp.Core

[<Literal>]
let connStr = @"Data Source=(localdb)\v11.0;Initial Catalog=tempdb;Integrated Security=True"
let friendlyName = "Duplicate Column Name Analyzer"
let allGoodMsg = "No duplicate column names found"

let flags =
    //Offenses: duplicate column names within the same table
    //Warnings: duplicate column names across tables
    //OK: no duplicate column names
    let getAllTableNames = new SqlCommandProvider<"SELECT TABLE_NAME
                                                    FROM INFORMATION_SCHEMA.TABLES
                                                    WHERE TABLE_TYPE = 'BASE TABLE'", connStr>(connStr)

    let getAllColsByTable = new SqlCommandProvider<"select COLUMN_NAME
                                                    from INFORMATION_SCHEMA.COLUMNS
                                                    where TABLE_NAME=@tName", connStr>(connStr)

    let getAllCols = new SqlCommandProvider<"select COLUMN_NAME, TABLE_NAME
                                                    from INFORMATION_SCHEMA.COLUMNS", connStr>(connStr)

    let allTableNames = getAllTableNames.Execute()

    //Offenses
    let offenses : seq<string> =
        allTableNames
        |> Seq.map ( fun tableName ->
                    let tableCols = getAllColsByTable.Execute(tName = tableName)
                                    |> Seq.map (fun colName -> colName.Value)
                                    |> Seq.map (fun c -> c.ToLower())

                    tableCols
                    |> Seq.groupBy id
                    |> Seq.filter( fun (_,set) -> (Seq.length set) > 1) //duplicate names
                    |> Seq.map( fun (key,_) -> key )
                    |> Seq.map (fun duplicateColumnName ->
                                    sprintf "Duplicate column names in Table %s: Columns %s" tableName duplicateColumnName)
            )
        |> Seq.concat

        
    //Warnings
    let warnings : seq<string> =
        let allCols = getAllCols.Execute()
        allCols
        |> Seq.groupBy (fun r -> r.COLUMN_NAME)
        |> Seq.filter( fun ( _ , set) -> (Seq.length set) > 1) //duplicate names
        |> Seq.map (fun (key, set) ->
                    let tables = set|> Seq.map (fun record -> record.TABLE_NAME) |> String.concat ", "
                    sprintf "Duplicate column name '%s' in tables: %s" key.Value tables
            )
    (offenses,warnings)

let getFinal() = (connStr, friendlyName, allGoodMsg, flags)
