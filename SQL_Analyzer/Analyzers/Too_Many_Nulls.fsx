#r @"..\..\packages\FSharp.Data.SqlClient.1.8.2\lib\net40\FSharp.Data.SqlClient.dll" //SQL Type Provider reference
#load "..\SQL_Typifier.fsx" //This file gives you the means to analyze databases

//open FSharp.Data //uncomment if you want to use the SQL Type Provider to execute your own SQL queries
open FSharp.Core
open System
open SQL_Typifier

[<Literal>]
let connStr = @"Data Source=(localdb)\v11.0;Initial Catalog=tempdb;Integrated Security=True" //CHANGE
let friendlyName = "Excessive NULL Analyzer" //CHANGE
let allGoodMsg = "No columns found with excessive nulls!" //CHANGE

let concatAll (ls: seq<string> list) : seq<string> = Seq.fold (fun acc elem -> Seq.append acc elem) (List.toSeq []) ls

let flags : seq<string> * seq<string> =
    //Offenses: Over 50% of a column is NULL
    //Warnings: Over 30% of a column is NULL
    //OK: Less than 30% of a column is NULL

    let allTableNames = SQL_Typifier.getAllTableNames connStr
    let allColumns = allTableNames |> List.collect (fun tName -> SQL_Typifier.getAllColumnInfoOfTable connStr tName 0 false)

    let offenses = allColumns
                    |> List.map (fun column ->
                        let colDataSize = column.columnData |> List.length
                        let nullsAmount = List.filter ((=) "<null>") column.columnData |> List.length

                        let nullRatio = (float nullsAmount) / (float colDataSize)

                        if nullRatio >= 0.5 then
                            [(sprintf "Column [%s].[%s] is more than half NULL" column.tableName column.columnName)] |> List.toSeq
                        else 
                            Seq.init 0 (fun int -> "")
                        )
                        |> concatAll

    let warnings = allColumns
                    |> List.map (fun column ->
                        let colDataSize = column.columnData |> List.length
                        let nullsAmount = List.filter ((=) "<null>") column.columnData |> List.length

                        let nullRatio = (float nullsAmount) / (float colDataSize)

                        if nullRatio < 0.5 && nullRatio > 0.3 then
                            [(sprintf "Column [%s].[%s] is more than thirty percent NULL" column.tableName column.columnName)] |> List.toSeq
                        else 
                            Seq.init 0 (fun int -> "")
                        )
                        |> concatAll
    (offenses,warnings)

let getFinal() = (connStr, friendlyName, allGoodMsg, flags) //DON'T TOUCH
