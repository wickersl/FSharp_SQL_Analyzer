namespace CoreWorkings
open System.IO
open CoreWorkings

module OutputWriter =
    let printOK (stream: StreamWriter) (okMsg: string) (analyzerName: string) =
        let write (s: string) = stream.WriteLine s
        write "#################################################"
        write analyzerName
        write "Analyzer found no errors!"
        write okMsg
        write "#################################################"
        write stream.NewLine

    let printBadThings (stream: StreamWriter) (analyzerName: string) (result: seq<string> * seq<string>) =
        let write (s: string) = stream.WriteLine s //easier writing

        write "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
        write analyzerName

        let offenses,warnings = result

        if Seq.length offenses > 0 then
            write "!!!OFFENSES FOUND!!!"
            let o = String.concat "\n" offenses
            write o
        else
            write "No offenses found. Great!"

        if Seq.length warnings > 0 then
            write "~~~WARNINGS FOUND~~~"
            let w = String.concat "\n" warnings //clean
            //printfn "hello: %s" w
            write w
        else
            write "No warnings found. Great!"

        write "!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!"
        write stream.NewLine

    let writeOutputFileHeader (w: StreamWriter) (connStr: string) = 
        w.WriteLine (sprintf "Database analyzed: %s"  connStr)
        w.WriteLine (sprintf "Time started: %A" System.DateTime.Now)


    let printToOutputFile (writer: StreamWriter) (analyzedResult: Analyzer) =
        match analyzedResult.Results with
        | OK okmsg -> printOK writer okmsg analyzedResult.FriendlyName
        | Flag (o, w) -> printBadThings writer analyzedResult.FriendlyName (o, w)

