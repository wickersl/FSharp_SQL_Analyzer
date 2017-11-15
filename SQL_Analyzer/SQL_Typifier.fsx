#r @"..\packages\FSharp.Compiler.Service.16.0.2\lib\net45\FSharp.Compiler.Service.dll"
#r @"..\packages\FSharp.Data.SqlClient.1.8.2\lib\net40\FSharp.Data.SqlClient.dll"
#load "MainRunner.fs"
open Microsoft.FSharp.Compiler.Interactive.Shell
open System.Text.RegularExpressions
open CoreWorkings
open FSharp.Data

type ColumnInfo = {
        tableName: string
        columnName: string
        datatype: string
        //columnData: ColumnData
        columnData: string list
    }
    
    //type RowDatum = ColumnName * DatabaseDatum
    type RowDatum = string * string

    type RowInfo = { //a single row
        tableName: string
        rowData: RowDatum list
    }

    type TableInfo = {
        tableName: string
        primaryKeys: string list
        allColumnInfo: ColumnInfo list option
        allRowInfo: RowInfo list option
    }

    type DatabaseInfo = {
        allTableInfo: TableInfo list
    }
    
//Needed to dynamically execute SQL queries
let makeCustomSelect (connectionString: string) (q: string) =
    System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)
    let tab s = "    " + s
    let path = @"Select_Query_Script.fsx"

    let w = new System.IO.StreamWriter(path)
    let write (s: string) = w.WriteLine s

    write "#r @\"..\packages\FSharp.Data.SqlClient.1.8.2\lib\\net40\FSharp.Data.SqlClient.dll\""
    write "open FSharp.Data"
    write w.NewLine

    write @"[<Literal>]"
    let c = "let connStr0 = @" + "\"" + connectionString + "\""
    write c

    let makeSCP = "let cmdSCP = new SqlCommandProvider<" + "\"" + q + "\"" + ", connStr0>(connStr0)"
    write makeSCP

    write "let x = cmdSCP.Execute() |> Seq.toList"
    write """let getSelection() = x |> List.map (fun o -> sprintf "%A" o)"""

    w.Close()


let (|Regex|_|) pattern input = //For pattern matching
    let m = Regex.Match(input, pattern)
    if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
    else None

let fsiValToStringList (fsiVal: FsiValue) = //pass to the evaluator so it converts the FsiValue to a usable type
    box fsiVal.ReflectionValue :?> string list
        
let parseDatum (s: string) = //parse the returned string into a more informative string
    match s with
    | Regex """(Some )("?([\d\w. -]+)"?)""" [some; valWithQuotes; value] -> value
    | Regex """("(\w+)")""" [stringDatumWithQuotes; stringDatum] -> stringDatum //add \d??
    | _ -> s

// ------------------------- PARSERS ------------------------------

let parseFsiValToString (fsiVal: FsiValue) = //pass to Evaluator so it returns a nice happy string
    fsiValToStringList fsiVal |> List.map (fun s -> parseDatum s)
    
let parseAllRowInfo (tableName: string) (fsiVal: FsiValue) =

    let parseSingleRow (row: string []) =
        let rowInfo =
                    row
                    |> Array.toList
                    |> List.map ( fun s -> match s with //list of string tuples
                                            | Regex """(\w+) = (Some )?("?([\d\w. -]+)"?)""" [colName; some; colValQuotes; colVal] -> 
                                                (colName,colVal)
                                            | _ -> ("ERROR", "COULD NOT PARSE ROW")
                    )
                    |> List.map (fun t -> match t with
                                            | x, "None " -> (x, "NULL")
                                            | x, "None" -> (x, "NULL")
                                            | x,y -> (x, parseDatum y)
        
                                    )
                    |> List.map (fun t -> //Make into RowDatum type
                                            let ret : RowDatum = t
                                            ret
                                    )
        {tableName = tableName; rowData = rowInfo}

    let parseThis = fsiVal.ReflectionValue :?> string list //each item is a row
    parseThis
    |> List.map (fun wholeRow -> wholeRow.Split [|';'|])
    |> List.map (fun row -> parseSingleRow row)
        

//---------------------------------------------------------------------
//-----------------------GET DATABASE INFORMATION----------------------

//Get the Primary Key of a table
let getPKOfTable (connStr: string) (tName: string) =
    let getPKsQuery = sprintf "SELECT Col.Column_Name from 
                                    INFORMATION_SCHEMA.TABLE_CONSTRAINTS Tab, 
                                    INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE Col 
                                WHERE 
                                    Col.Constraint_Name = Tab.Constraint_Name
                                    AND Col.Table_Name = Tab.Table_Name
                                    AND Constraint_Type = 'PRIMARY KEY'
                                    AND Col.Table_Name = '%s'" tName
    makeCustomSelect connStr getPKsQuery //let tName = "Person"
    match (Evaluator.evaluateSelectQuery "Select_Query_Script.fsx" parseFsiValToString) with
    | Some c -> c
    | None -> ["ERROR: PRIMARY KEYS NOT READ"]
       
//Get a list of all table names of a database
let getAllTableNames (connStr: string) =
    let getAllTableNamesQuery = "SELECT TABLE_NAME
                                    FROM INFORMATION_SCHEMA.TABLES
                                    WHERE TABLE_TYPE = 'BASE TABLE'"
    makeCustomSelect connStr getAllTableNamesQuery

    match (Evaluator.evaluateSelectQuery "Select_Query_Script.fsx" parseFsiValToString) with
    | Some r -> r |> List.map (fun tName -> tName.Replace("\"",""))
    | None -> ["ERROR: TABLE NAMES NOT READ"]

//Get a list of all the column names of a table
let getAllColNamesOfTable (connStr: string) (tName: string) =
    let getAllColsByTableQuery tName = sprintf "select COLUMN_NAME
                                            from INFORMATION_SCHEMA.COLUMNS
                                            where TABLE_NAME=\'%s\'" tName
    makeCustomSelect connStr (getAllColsByTableQuery tName)
    match (Evaluator.evaluateSelectQuery "Select_Query_Script.fsx" parseFsiValToString) with
    | Some r -> r |> List.map (fun tName -> tName.Replace("\"",""))
    | None -> ["ERROR: TABLE NAMES NOT READ"]

//Get the datatype of a column
let getDatatypeOfColumn (connStr: string) (tName: string) (colName: string) =
    let getDatatypeQuery = sprintf "SELECT DATA_TYPE
                        FROM Information_schema.Columns
                        WHERE TABLE_NAME = '%s' and COLUMN_NAME = '%s'" tName colName
    makeCustomSelect connStr getDatatypeQuery
    match (Evaluator.evaluateSelectQuery "Select_Query_Script.fsx" parseFsiValToString) with
    | Some c -> c
    | None -> ["ERROR: DATATYPE NOT READ"]

//---------------------------------------------------------------------
//-----------------------STUFF THE USER WANTS--------------------------

//Get a ColumnInfo, given the table name and the column name
let getColumnInfo (connStr: string) (tableName: string) (colName: string) (numberOfItems: int) (percent: bool) : ColumnInfo =
    let num =
        //if numberOfItems = 0 then "*"
        if numberOfItems < 0 then sprintf "%i" (numberOfItems * -1)
        else sprintf "%i" numberOfItems

    let selectQuery = if numberOfItems = 0 then sprintf "SELECT %s FROM %s" colName tableName
                        else if percent then sprintf "SELECT TOP %s PERCENT %s FROM %s" num colName tableName
                        else sprintf "SELECT TOP %s %s FROM %s" num colName tableName

    makeCustomSelect connStr selectQuery //make the file

    let colContents = match (Evaluator.evaluateSelectQuery "Select_Query_Script.fsx" parseFsiValToString) with
                        | Some c -> c
                        | None -> ["ERROR: COL DATA NOT READ"]
    let dt = (getDatatypeOfColumn connStr tableName colName ) |> List.head //only one datatype
    {tableName = tableName; columnName = colName; datatype = dt;columnData = colContents} //ColumnInfo

let getAllColumnInfoOfTable (connStr: string) (tableName: string) (depth: int) (percent: bool) : ColumnInfo list =
    let allColNames = getAllColNamesOfTable connStr tableName
    allColNames
    |> List.map (fun col ->
            getColumnInfo connStr tableName col depth percent
        )

//Get a list of RowInfo for a table
let getAllRowInfoOfTable (connStr: string) (tableName: string) (depth: int) (percent: bool) : RowInfo list =
    let num =
        if depth = 0 then "*"
        else if depth < 0 then sprintf "%i" (depth * -1)
        else sprintf "%i" depth

    let selectQuery = if percent then sprintf "SELECT TOP %s PERCENT * FROM %s" num tableName
                        else sprintf "SELECT TOP %s * FROM %s" num tableName
    let parser = parseAllRowInfo tableName

    makeCustomSelect connStr selectQuery //make the file
    match (Evaluator.evaluateSelectQuery "Select_Query_Script.fsx" parser) with
    | Some r -> r
    | None -> [{tableName="ERROR"; rowData=[("ERROR","ERROR")]}]
                            

//DatabaseInfo with columns 
let getDatabaseInfoVertical (connStr: string) (depth: int) (percent: bool) : DatabaseInfo =
    let allTableNames = getAllTableNames connStr

    let allTables =
        allTableNames
        |> List.map (fun table ->
                        let allColNames = getAllColNamesOfTable connStr table
                        let allCols =
                                allColNames
                                |> List.map (fun col ->
                                        getColumnInfo connStr table col depth percent)
                        let pks = getPKOfTable connStr table
                        let tableInfo : TableInfo = {tableName = table; primaryKeys = pks; allColumnInfo = Some allCols; allRowInfo = None}
                        tableInfo
                    )
    {allTableInfo = allTables}
    
//DatabaseInfo with rows
let getDatabaseInfoHorizontal (connStr: string) (depth: int) (percent: bool) : DatabaseInfo = 
    let allTableNames = getAllTableNames connStr

    let allTables =
        allTableNames
        |> List.map (fun table ->
                let allRowInfo = getAllRowInfoOfTable connStr table depth percent
                let pks = getPKOfTable connStr table
                {tableName = table; primaryKeys = pks; allColumnInfo = None; allRowInfo = Some allRowInfo}
            )
    {allTableInfo = allTables}

//DatabaseInfo with only table names and their PKs
let getDatabaseInfoEmpty (connStr: string) : DatabaseInfo = 
    let allTableNames = getAllTableNames connStr
    let allTables =
        allTableNames
        |> List.map (fun table ->
                let pks = getPKOfTable connStr table
                {tableName = table; primaryKeys = pks; allColumnInfo = None; allRowInfo = None}
            )
    {allTableInfo = allTables}

//Get a single TableInfo populated by its columns
let getTableInfoVertical (connStr: string) (tableName: string) (depth: int) (percent: bool) : TableInfo =
    let pks = getPKOfTable connStr tableName
    let allColNames = getAllColNamesOfTable connStr tableName
    let allCols =
        allColNames
        |> List.map (fun col -> getColumnInfo connStr tableName col depth percent)
    {tableName = tableName; primaryKeys = pks; allColumnInfo = Some allCols; allRowInfo = None}

//Get a single TableInfo populated by its rows
let getTableInfoHorizontal (connStr: string) (tableName: string) (depth: int) (percent: bool) : TableInfo =
    let pks = getPKOfTable connStr tableName
    let rowInfo = getAllRowInfoOfTable connStr tableName depth percent
    {tableName = tableName; primaryKeys = pks; allColumnInfo = None; allRowInfo = Some rowInfo}
