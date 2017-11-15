#r @"..\..\packages\FSharp.Data.SqlClient.1.8.2\lib\net40\FSharp.Data.SqlClient.dll" //SQL Type Provider reference
#load "..\SQL_Typifier.fsx" //This file gives you the means to analyze databases

//open FSharp.Data //uncomment if you want to use the SQL Type Provider to execute your own SQL queries
open FSharp.Core
open System
open SQL_Typifier

[<Literal>]
let connStr = @"DATABASE CONNECTION STRING" //CHANGE
let friendlyName = "NAME YOUR ANALYZER" //CHANGE
let allGoodMsg = "MESSAGE TO PRINT WHEN EVERYTHING ANALYZER DOESN'T FIND ANYTHING" //CHANGE

//dbContext is your window into the database. SQL_Typifier has several means of providing info.
//pick one that best suits what you're trying to analyze.
//'Vertical' means you get information about individual columns
//'Horizontal' means you get entire table rows
let dbContext = SQL_Typifier.getDatabaseInfoVertical connStr 1 false //CHANGE IF NEEDED

//If you map your list of TableInfo's into a list of warnings/offenses, you'll need to concatenate
//the warnings/offenses (seq<string>) for each table into a single long seq<string> for the whole database.
//Forward pipe your 'seq<string> list' into this function and get the appropriate return type
//for 'warnings' (seq<string>)
let concatAll (ls: seq<string> list) : seq<string> = Seq.fold (fun acc elem -> Seq.append acc elem) (List.toSeq []) ls

let flags : seq<string> * seq<string> = //First one is Offenses, second is Warnings
    //Offenses: <detail what counts as an offense>
    //Warnings: <detail what counts as a warning>
    //OK: <detail what means nothing is wrong with the database according to the analyzer>

    
    //Offenses
    let offenses : seq<string> =
        Seq.init 0 (fun int -> "") //seq<string> with nothing in it

        
    //Warnings
    let warnings : seq<string> =
        Seq.init 0 (fun int -> "")
    
    
    (offenses,warnings) //comma designates a tuple in practice. asterisk designates a tuple in a type

let getFinal() = (connStr, friendlyName, allGoodMsg, flags) //DON'T TOUCH
