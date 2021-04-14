namespace TSMPModdedSIILauncher.Views

open Avalonia
open Avalonia.Controls
open Avalonia.Controls.Presenters
open Avalonia.Layout
open Avalonia.Media
open Avalonia.Styling

open Elmish
open Avalonia.FuncUI.DSL
open CmlLib.Core.Auth

module Login =
    type Msg = 
        | SetEmail of string
        | SetPassword of string
        | AttemptLogin
        | ContinueWithoutLogin
        | LoginSuccess of MSession
        | LoginFailed of string
        | AttemptAutoLogin
    type State =
      { email: string
        password: string
        errorMessage: string}

    let init () =
      { email = ""
        password = ""
        errorMessage = "" }, Cmd.ofMsg AttemptAutoLogin

    let update (msg: Msg) (state: State): State * Cmd<Msg> =
        match msg with 
        | SetEmail newEmail -> {state with email = newEmail }, Cmd.none
        | SetPassword newPassword -> {state with password = newPassword; }, Cmd.none
        | AttemptLogin -> 
            // try to login 
            let response = MLogin().Authenticate(state.email,state.password)

            let command = if response.IsSuccess then LoginSuccess ( response.Session ) else LoginFailed $"%O{response.Result} %s{response.ErrorMessage}"
            state, Cmd.ofMsg command

        | ContinueWithoutLogin -> state, Cmd.ofMsg ( LoginSuccess ( MSession.GetOfflineSession("offline") ))
        | LoginSuccess _ -> init ()
        | LoginFailed message -> {state with errorMessage = message}, Cmd.none
        | AttemptAutoLogin -> 
            let response = MLogin().TryAutoLogin();
            state, (if response.IsSuccess then Cmd.ofMsg (LoginSuccess response.Session) else Cmd.none)

    let view (state: State) dispatch =
        StackPanel.create [
            StackPanel.horizontalAlignment HorizontalAlignment.Center
            StackPanel.verticalAlignment VerticalAlignment.Center
            StackPanel.children [
                TextBlock.create [
                    TextBlock.fontWeight FontWeight.Medium
                    TextBlock.fontSize 25.
                    TextBlock.text "TSMP Modded Season II"
                    TextBlock.foreground "#1d2342"
                ]
                TextBlock.create [
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.fontWeight FontWeight.Normal
                    TextBlock.fontSize 20.
                    TextBlock.text "Minecraft Mojang Login"
                    TextBlock.foreground "#1d2342"
                    TextBlock.fontStyle FontStyle.Italic
                    TextBlock.padding (Thickness(20.0))
                ]
                TextBox.create [
                    TextBox.text state.email
                    TextBox.watermark "email"
                    TextBox.width 250.
                    TextBox.margin 10.
                    TextBox.padding 10.
                    TextBox.fontSize 14.
                    TextBox.onTextChanged (SetEmail >> dispatch)
                ]
                        
                TextBox.create [
                    TextBox.text state.password
                    TextBox.watermark "password"
                    TextBox.width 250.
                    TextBox.margin 10.
                    TextBox.padding 10.
                    TextBox.fontSize 14.
                    TextBox.passwordChar '*'
                    TextBox.onTextChanged (SetPassword >> dispatch)
                ]
                Button.create [
                    Button.content "Login"
                    Button.background "#7332e3"
                    Button.foreground "white"
                    Button.height 30.
                    Button.fontSize 14.
                    Button.onClick (fun _ -> AttemptLogin |> dispatch)
                    Button.isEnabled ((state.password <> "") && (state.email <> ""))
                    Button.styles (
                        let styles = Styles()
                        let style = Style(fun x -> x.OfType<Button>().Template().OfType<ContentPresenter>())
    
                        let setter = Setter(ContentPresenter.CornerRadiusProperty, CornerRadius(18.0))
                        style.Setters.Add setter
     
                        styles.Add style
                        styles
                    )
                ]
                TextBlock.create [
                    TextBlock.text "Continue without logging in"
                    TextBlock.fontStyle FontStyle.Oblique
                    TextBlock.textDecorations TextDecorations.Underline
                    TextBlock.fontSize 13.
                    TextBlock.horizontalAlignment HorizontalAlignment.Center
                    TextBlock.fontWeight FontWeight.Medium
                    TextBlock.foreground "deepskyblue"
                    TextBlock.onTapped (fun _ -> ContinueWithoutLogin |> dispatch)
                ]
                TextBlock.create [
                    TextBlock.maxLines 4
                    TextBlock.text state.errorMessage
                    TextBlock.foreground "red"
                    TextBlock.fontSize 13.
                ]
            ]
        ]     

