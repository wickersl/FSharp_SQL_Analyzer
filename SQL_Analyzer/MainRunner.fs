namespace CoreWorkings
//module MainRunner

//open CoreWorkings
//open TypeContainer
open System.IO
open System

type Results =
    | Flag of seq<string> * seq<string> //i hate this
    | OK of string

type Analyzer = {
        ConnectionString: string
        Results: Results
        FriendlyName: string
    }
    

module Evaluator =
    open System.Globalization
    open System.Text
    open Microsoft.FSharp.Compiler.Interactive.Shell
    open System.Data.Common

    let sbOut = StringBuilder()
    let sbErr = StringBuilder()

    let fsi =
        let inStream = new StringReader("")
        let outStream = new StringWriter(sbOut)
        let errStream = new StringWriter(sbErr)
        try
            let fsiConfig = FsiEvaluationSession.GetDefaultConfiguration()
            let argv = [| "/temp/fsi.exe"; |] //temo? change to temp?
            FsiEvaluationSession.Create(fsiConfig, argv, inStream, outStream, errStream)
        with
        | ex ->
            printfn "Error: %A" ex
            printfn "Inner: %A" ex.InnerException
            printfn "ErrorStream: %s" (errStream.ToString())

            raise ex
            

    let getOpen path =
        let path = Path.GetFullPath path
        let filename = Path.GetFileNameWithoutExtension path
        let textInfo = (CultureInfo("en-US", false)).TextInfo
        textInfo.ToTitleCase filename

    let getLoad path =
         let path = Path.GetFullPath path
         path.Replace("\\", "\\\\")

    let getAnalyzers (f: FsiValue) : Analyzer list = //ohhhhh goddddddd
        let results = f.ReflectionValue :?> list<string * string * string * (seq<string> * seq<string>)> //HIDEOUS... but necessary
        results
        |> List.map (fun res ->
            match res with
            | connStr,friendlyName,allGood,flags ->
                let offenses,warnings = flags
                let finalFlag =
                    if Seq.length offenses = 0 && Seq.length warnings = 0 then
                        OK allGood
                    else
                        Flag flags
                //printfn "Converted warnings: %A" warnings
                {ConnectionString = connStr; Results = finalFlag; FriendlyName = friendlyName}

            )

    let evaluateAnalyzers path =
        let filename = getOpen path
        let load = getLoad path

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        if errs.Length > 0 then printfn "Load Errors : %A" errs

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        if errs.Length > 0 then printfn "Open Errors : %A" errs

        let res,errs = fsi.EvalExpressionNonThrowing "getAllResults()" //name of core function for analyzers
        if errs.Length > 0 then printfn "getFinal Errors : %A" errs

        match res with
        | Choice1Of2 (Some f) ->
            getAnalyzers f |> Some
        | _ -> None

    let evaluateSelectQuery path parser =
        let filename = getOpen path
        let load = getLoad path

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "#load \"%s\";;" load)
        if errs.Length > 0 then printfn "Load Errors : %A" errs

        let _, errs = fsi.EvalInteractionNonThrowing(sprintf "open %s;;" filename)
        if errs.Length > 0 then printfn "Open Errors : %A" errs

        let res,errs = fsi.EvalExpressionNonThrowing "getSelection()"
        if errs.Length > 0 then printfn "getSelection Errors : %A" errs

        match res with
        | Choice1Of2 (Some f) ->
            parser f |> Some
        | _ -> None

