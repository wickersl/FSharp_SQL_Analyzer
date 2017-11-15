#r @"..\..\packages\FSharp.Data.SqlClient.1.8.2\lib\net40\FSharp.Data.SqlClient.dll"
#load "..\SQL_Typifier.fsx"

//open FSharp.Data
open FSharp.Core
open System
open SQL_Typifier

[<Literal>]
let connStr = @"Data Source=(localdb)\v11.0;Initial Catalog=tempdb;Integrated Security=True"
let friendlyName = "Compound data analyzer"
let allGoodMsg = "No columns found with suspected compound data"

let dbContext = SQL_Typifier.getDatabaseInfoVertical connStr 1 false

let flags =
    //Offenses: None
    //Warnings: Columns with data that seems split into two parts that might be best split into two different cols
    //OK: nothing suspicious
    let textTypes = ["varchar"; "nvarchar"; "char"; "nchar"; "text"; "ntext"]
    let separatorChars = ["_";"/";"-";".";","]

    let isStringClean (s: string) : bool =
        separatorChars
        |> List.map (fun sep -> not (s.Contains sep)) //if all true, the string is clean
        |> List.fold (fun acc elem -> acc && elem) true

    let isColDirty (c: ColumnInfo) : bool =
        not (isStringClean (c.columnData |> List.head))
    
    //Offenses
    let offenses : seq<string> =
        Seq.init 0 (fun int -> "") //empty because it's hard to objectively tell if a column has compound data

        
    //Warnings
    let warnings : seq<string> =
        let isColumnTextType (col: ColumnInfo) : bool =
            List.exists ((=) col.datatype) textTypes

        let concatAll (ls: seq<string> list) = Seq.fold (fun acc elem -> Seq.append acc elem) (List.toSeq []) ls

        dbContext.allTableInfo //list of seq<string> -> seq<string>
        |> List.map (fun tableInfo ->
            tableInfo.allColumnInfo.Value // We know there's allColumnInfo, so .Value gets it from the option type
            |> Seq.filter isColumnTextType //Only care about text-type columns
            |> Seq.filter isColDirty //get ColumnInfos with dirty strings
            |> Seq.map (fun dirtyColumn -> sprintf "Potential compound data found in column [%s].[%s]" tableInfo.tableName dirtyColumn.columnName)
            )
        |> concatAll
    
    
    (offenses,warnings)

let getFinal() = (connStr, friendlyName, allGoodMsg, flags)
