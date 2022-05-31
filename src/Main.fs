module Main

open Feliz
open Browser.Dom

ReactDOM.render(
    App.App()
    ,
    document.getElementById("root")
)
