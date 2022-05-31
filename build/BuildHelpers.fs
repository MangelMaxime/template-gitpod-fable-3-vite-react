module BuildHelpers

open Fake.Core
open BlackFox

module Proc =
    open System
    open System.Text

    let onStdout (sb : StringBuilder) (line: string) =
        sb.AppendLine line |> ignore
        if String.isNotNullOrEmpty line then
            Console.WriteLine line

    let onStderr (sb : StringBuilder) (line: string) =
        sb.AppendLine line |> ignore
        if String.isNotNullOrEmpty line then
            let currentColor = Console.ForegroundColor
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine line
            Console.ForegroundColor <- currentColor

    let runWithCaptureOutputAndRedirect createProcess =
        let stdOut = StringBuilder()
        let stdErr = StringBuilder()

        createProcess
        |> CreateProcess.redirectOutputIfNotRedirected
        |> CreateProcess.withOutputEvents (onStdout stdOut) (onStderr stdErr)
        |> Proc.run
        |> ignore

        stdOut.ToString(), stdErr.ToString()

    module Parallel =
        let locker = obj()

        let colors =
            [| ConsoleColor.Blue
               ConsoleColor.Yellow
               ConsoleColor.Magenta
               ConsoleColor.Cyan
               ConsoleColor.DarkBlue
               ConsoleColor.DarkYellow
               ConsoleColor.DarkMagenta
               ConsoleColor.DarkCyan |]

        let print color (colored: string) (line: string) =
            lock locker
                (fun () ->
                    let currentColor = Console.ForegroundColor
                    Console.ForegroundColor <- color
                    Console.Write colored
                    Console.ForegroundColor <- currentColor
                    Console.WriteLine line)

        let onStdout index name (line: string) =
            let color = colors.[index % colors.Length]
            if isNull line then
                print color $"{name}: --- END ---" ""
            else if String.isNotNullOrEmpty line then
                print color $"{name}: " line

        let onStderr name (line: string) =
            let color = ConsoleColor.Red
            if isNull line |> not then
                print color $"{name}: " line

        let redirect (index, (name, createProcess)) =
            createProcess
            |> CreateProcess.redirectOutputIfNotRedirected
            |> CreateProcess.withOutputEvents (onStdout index name) (onStderr name)

        let printStarting indexed =
            for (index, (name, c: CreateProcess<_>)) in indexed do
                let color = colors.[index % colors.Length]
                let wd =
                    c.WorkingDirectory
                    |> Option.defaultValue ""
                let exe = c.Command.Executable
                let args = c.Command.Arguments.ToStartInfo
                print color $"{name}: {wd}> {exe} {args}" ""

        let run cs =
            cs
            |> Seq.toArray
            |> Array.indexed
            |> fun x -> printStarting x; x
            |> Array.map redirect
            |> Array.Parallel.map Proc.run

let createProcess exe arg dir =
    CreateProcess.fromRawCommandLine exe arg
    |> CreateProcess.withWorkingDirectory dir
    |> CreateProcess.ensureExitCode

let run proc arg dir =
    proc arg dir
    |> Proc.run
    |> ignore

let runParallel processes =
    processes
    |> Proc.Parallel.run
    |> ignore

let runOrDefault defTarget args =
    try
        match args with
        | [| target |] -> Target.runOrDefault target
        | _ -> Target.runOrDefault defTarget
        0
    with e ->
        printfn "%A" e
        1

let npm =
    match PathEnvironment.findExecutable "npm" false with
    | None ->
        failwith "npm was not found in path. Please install it and make sure it's available from your path."
    | Some npm ->
        createProcess npm

let dotnet = createProcess "dotnet"

let npx =
    match PathEnvironment.findExecutable "npx" false with
    | None ->
        failwith "npx was not found in path. Please install it and make sure it's available from your path."
    | Some npx ->
        createProcess npx

let git = createProcess "git"

module Glob =

    open Fake.IO.FileSystemOperators

    let fableJs baseDir = baseDir </> "**/*.fs.js"
    let fableJsMap baseDir = baseDir </> "**/*.fs.js.map"
    let js baseDir = baseDir </> "**/*.js"
    let jsMap baseDir = baseDir </> "**/*.js.map"
