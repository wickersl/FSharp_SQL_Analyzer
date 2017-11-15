// Learn more about F# at http://fsharp.org
// See the 'F# Tutorial' project for more help.
open System.IO
open CoreWorkings
open OutputWriter
//open TypeContainer

type AnalyzerResults =
        | Analyzers of Analyzer list
        | Error of string
 
 [<EntryPoint>]
let main argv = //argv0 is path to analyzers, argv1 is path to output file
    System.IO.Directory.SetCurrentDirectory (__SOURCE_DIRECTORY__)
    //let pathToAnalyzers = Array.head argv
    //let outputFilePath = Array.tail argv |> Array.head
    let pathToMain = "Analyzers\\Main.fsx"
    let outputFilePath = "output\\output.txt"

    let writer : StreamWriter = new StreamWriter(outputFilePath)

    let analyzerResults : AnalyzerResults = match Evaluator.evaluateAnalyzers pathToMain with
                                            | Some analyzers -> Analyzers analyzers
                                            | None -> Error (sprintf "File `%s` couldn't be parsed" pathToMain)

    match analyzerResults with
    | Analyzers analyzers ->
            let testFirst = match List.tryHead analyzers with
                            | Some a -> a.ConnectionString
                            | None -> "Error: Need at least one analyzer"
            writeOutputFileHeader writer testFirst
            analyzers |> List.map (fun analyzer -> printToOutputFile writer analyzer)
    | Error errorString -> [printfn "%s" errorString] //so dumb but hey the types gotta match
    |> ignore
    writer.Close()

    0