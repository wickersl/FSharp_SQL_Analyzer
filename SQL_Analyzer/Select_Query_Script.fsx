#r @"..\packages\FSharp.Data.SqlClient.1.8.2\lib\net40\FSharp.Data.SqlClient.dll"
open FSharp.Data


[<Literal>]
let connStr0 = @"Data Source=(localdb)\v11.0;Initial Catalog=tempdb;Integrated Security=True"
let cmdSCP = new SqlCommandProvider<"SELECT DATA_TYPE
                        FROM Information_schema.Columns
                        WHERE TABLE_NAME = 'Pets' and COLUMN_NAME = 'Owner'", connStr0>(connStr0)
let x = cmdSCP.Execute() |> Seq.toList
let getSelection() = x |> List.map (fun o -> sprintf "%A" o)
