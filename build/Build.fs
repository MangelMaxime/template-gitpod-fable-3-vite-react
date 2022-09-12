open Fake.Core
open Fake.IO
open Fake.IO.FileSystemOperators
open Fake.IO.Globbing.Operators
open BlackFox.Fake
open BuildHelpers
open System

let cwd = Environment.CurrentDirectory

let srcPath = Path.getFullName "src"

let gitOwner = "glutinum-org"
let gitName = "Glutinum.Feliz.ReactResizeDetector"

[<EntryPoint>]
let main args =
    BuildTask.setupContextFromArgv args

    let clean = BuildTask.create "Clean" [] {
        [
            srcPath </> "bin"
            srcPath </> "obj"
        ]
        |> Shell.cleanDirs

        !! (Glob.fableJs srcPath)
        |> Seq.iter Shell.rm
    }

    let npmInstall = BuildTask.create "NpmInstall" [] {
        run npm "install" cwd
    }

    let watch = BuildTask.create "Watch" [ npmInstall; clean ] {
        // All for graceful shutdown on Ctrl+C while the processes are running
        Console.CancelKeyPress.AddHandler(fun _ ea ->
            ea.Cancel <- true
            printfn "Received Ctrl+C, shutting down..."
            Environment.Exit(0)
        )

        [
            "vite", npx "vite dev" srcPath
            "fable", dotnet "fable --watch" srcPath
        ]
        |> runParallel
    }

    let build = BuildTask.create "Build" [ npmInstall; clean ] {
        run dotnet "fable" srcPath
        run npx "vite build" srcPath
    }

    BuildTask.runOrList ()
    0
