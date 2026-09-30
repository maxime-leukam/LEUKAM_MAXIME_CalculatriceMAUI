using System.Globalization;

namespace CalculatriceMAUI;

public partial class MainPage : ContentPage
{
    private const int MaxChiffres = 15;

    private string _current = "0";          // saisie ou resultat courant (point decimal interne)
    private double? _left;                  // operande de gauche (valeur)
    private string? _op;                    // operateur en attente : + - * / ^
    private bool _newEntry = true;          // la prochaine saisie remplace la valeur courante
    private bool _error;                    // etat d'erreur
    private bool _apresEgal;                // vrai juste apres un "="
    private bool _degres = true;            // mode DEG (vrai) ou RAD (faux)
    private bool _sciVisible;               // panneau scientifique affiche ou non

    private string _expression = "";        // texte deja valide, ex. "2 + "
    private string _expressionFinale = "";  // texte affiche apres "=", ex. "2 + 2 ="
    private string? _fonctionTexte;         // texte de l'operande issu d'une fonction, ex. "√(9)"

    public MainPage()
    {
        InitializeComponent();
    }

    //  Mise en page : portrait / paysage + panneau Sci
    private void OnPageSizeChanged(object? sender, EventArgs e) => ApplyLayout();

    private void OnToggleSci(object? sender, EventArgs e)
    {
        _sciVisible = !_sciVisible;
        ApplyLayout();
    }

    private void ApplyLayout()
    {
        if (Width <= 0 || Height <= 0) return;

        bool paysage = Width > Height;
        SciPanel.IsVisible = _sciVisible;

        RootGrid.RowDefinitions.Clear();
        RootGrid.ColumnDefinitions.Clear();

        if (paysage)
        {
            RootGrid.RowSpacing = 6;
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(1, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(3, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(2, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(3, GridUnitType.Star)));

            Place(Affichage, 0, 0, 1, 2);
            Place(SciPanel, 1, 0, 1, 1);
            if (_sciVisible) Place(Clavier, 1, 1, 1, 1);
            else Place(Clavier, 1, 0, 1, 2);

            ResultLabel.FontSize = 34;
        }
        else
        {
            RootGrid.RowSpacing = _sciVisible ? 8 : 4;
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(2, GridUnitType.Star)));
            RootGrid.RowDefinitions.Add(new RowDefinition(_sciVisible ? new GridLength(2, GridUnitType.Star) : new GridLength(0)));
            RootGrid.RowDefinitions.Add(new RowDefinition(new GridLength(5, GridUnitType.Star)));
            RootGrid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));

            Place(Affichage, 0, 0, 1, 1);
            Place(SciPanel, 1, 0, 1, 1);
            Place(Clavier, 2, 0, 1, 1);

            ResultLabel.FontSize = 48;
        }
    }

    private static void Place(View v, int row, int col, int rowSpan, int colSpan)
    {
        Grid.SetRow(v, row);
        Grid.SetColumn(v, col);
        Grid.SetRowSpan(v, rowSpan);
        Grid.SetColumnSpan(v, colSpan);
    }

    //  Saisie : chiffres et virgule
    private void OnDigit(object? sender, EventArgs e)
    {
        PrepareNouvelleSaisie();
        string d = ((Button)sender!).Text;

        if (_newEntry || _current == "0")
        {
            _current = d;
            _newEntry = false;
        }
        else if (_current == "-0")
        {
            _current = "-" + d;
        }
        else if (_current.Replace("-", "").Replace(".", "").Length < MaxChiffres)
        {
            _current += d;
        }
        UpdateDisplay();
    }

    private void OnDecimal(object? sender, EventArgs e)
    {
        PrepareNouvelleSaisie();

        if (_newEntry)
        {
            _current = "0.";
            _newEntry = false;
        }
        else if (!_current.Contains('.'))
        {
            _current += ".";
        }
        UpdateDisplay();
    }

    // Appelée avant toute saisie de chiffre : sort de l'etat d'erreur ou d'apres "="
    private void PrepareNouvelleSaisie()
    {
        if (_error) ResetAll();

        if (_apresEgal)
        {
            _apresEgal = false;
            _expression = "";
            _expressionFinale = "";
            _op = null;
            _left = null;
            _newEntry = true;
        }

        if (_fonctionTexte != null)
        {
            _fonctionTexte = null;
            _newEntry = true;
        }
    }

    //  Operateurs et egal
    private void OnOperator(object? sender, EventArgs e)
    {
        if (_error) return;
        string op = (string)((Button)sender!).CommandParameter;

        // Apres "=", on repart du resultat affiche
        if (_apresEgal)
        {
            _apresEgal = false;
            _expression = "";
            _expressionFinale = "";
            _op = null;
            _left = Lire(_current);
            _fonctionTexte = null;
        }

        // Changement d'operateur sans nouveau nombre : on remplace l'ancien
        if (_op != null && _newEntry && _fonctionTexte == null)
        {
            _expression = _expression[..^3] + " " + Symbole(op) + " ";
            _op = op;
            UpdateDisplay();
            return;
        }

        string operande = OperandeEnCours();

        if (_op != null)
        {
            if (!Calculer()) return;   // enchainement : 12 + 7 x ...
        }
        else
        {
            _left = Lire(_current);
        }

        _expression += operande + " " + Symbole(op) + " ";
        _op = op;
        _fonctionTexte = null;
        _newEntry = true;
        UpdateDisplay();
    }

    private void OnEquals(object? sender, EventArgs e)
    {
        if (_error) return;

        string operande = OperandeEnCours();

        if (_op == null)
        {
            // Pas d'operation en attente : seulement utile pour valider une fonction (ex. √(9) =)
            if (_fonctionTexte == null) return;
            _expressionFinale = operande + " =";
            _fonctionTexte = null;
            _apresEgal = true;
            _newEntry = true;
            UpdateDisplay();
            return;
        }

        string final = _expression + operande + " =";

        if (!Calculer()) return;

        _expressionFinale = final;
        _expression = "";
        _op = null;
        _left = null;
        _fonctionTexte = null;
        _apresEgal = true;
        _newEntry = true;
        UpdateDisplay();
    }

    //  Fonctions avancées (un seul nombre)
    private void OnFonction(object? sender, EventArgs e)
    {
        if (_error) return;
        string key = (string)((Button)sender!).CommandParameter;

        if (_apresEgal)
        {
            _apresEgal = false;
            _expression = "";
            _expressionFinale = "";
            _op = null;
            _left = null;
        }

        double x = Lire(_current);
        string inner = OperandeEnCours();
        double rad = _degres ? x * Math.PI / 180.0 : x;
        double r;
        string texte;

        switch (key)
        {
            case "pi":
                r = Math.PI;
                texte = "π";
                break;

            case "sqrt":
                if (x < 0) { AfficherErreur("Racine d'un nombre négatif impossible"); return; }
                r = Math.Sqrt(x);
                texte = $"√({inner})";
                break;

            case "sqr":
                r = x * x;
                texte = $"({inner})²";
                break;

            case "inv":
                if (x == 0) { AfficherErreur("Division par zéro impossible"); return; }
                r = 1.0 / x;
                texte = $"1/({inner})";
                break;

            case "sin":
                r = Math.Sin(rad);
                texte = $"sin({inner})";
                break;

            case "cos":
                r = Math.Cos(rad);
                texte = $"cos({inner})";
                break;

            case "tan":
                if (Math.Abs(Math.Cos(rad)) < 1e-10) { AfficherErreur("Tangente indéfinie"); return; }
                r = Math.Tan(rad);
                texte = $"tan({inner})";
                break;

            case "ln":
                if (x <= 0) { AfficherErreur("Logarithme indéfini"); return; }
                r = Math.Log(x);
                texte = $"ln({inner})";
                break;

            case "log":
                if (x <= 0) { AfficherErreur("Logarithme indéfini"); return; }
                r = Math.Log10(x);
                texte = $"log({inner})";
                break;

            case "fact":
                if (x < 0 || x != Math.Floor(x) || x > 170)
                {
                    AfficherErreur("Factorielle non définie");
                    return;
                }
                r = 1;
                for (int i = 2; i <= (int)x; i++) r *= i;
                texte = $"({inner})!";
                break;

            default:
                return;
        }

        if (double.IsNaN(r) || double.IsInfinity(r))
        {
            AfficherErreur("Résultat invalide");
            return;
        }

        _current = Formater(r);
        _fonctionTexte = texte;
        _newEntry = true;
        UpdateDisplay();
    }

    private void OnDegRad(object? sender, EventArgs e)
    {
        _degres = !_degres;
        ModeLabel.Text = _degres ? "DEG" : "RAD";
    }

    //  Actions : C, effacement, signe, pourcentage
    private void OnClear(object? sender, EventArgs e)
    {
        ResetAll();
        UpdateDisplay();
    }

    private void OnBackspace(object? sender, EventArgs e)
    {
        if (_error)
        {
            ResetAll();
            UpdateDisplay();
            return;
        }

        // Apres "=" : on efface la ligne de l'operation
        if (_apresEgal)
        {
            _apresEgal = false;
            _expressionFinale = "";
            UpdateDisplay();
            return;
        }

        // Operande issu d'une fonction : on l'annule
        if (_fonctionTexte != null)
        {
            _fonctionTexte = null;
            _current = "0";
            _newEntry = false;
            UpdateDisplay();
            return;
        }

        if (_newEntry) return;

        _current = _current.Length > 1 ? _current[..^1] : "0";
        if (_current == "-") _current = "0";
        UpdateDisplay();
    }

    private void OnSign(object? sender, EventArgs e)
    {
        if (_error) return;

        if (_apresEgal)
        {
            _apresEgal = false;
            _expression = "";
            _expressionFinale = "";
            _op = null;
            _left = null;
        }

        // Juste apres un operateur : on commence un second operande negatif
        if (_newEntry && _op != null && _fonctionTexte == null)
        {
            _current = "-0";
            _newEntry = false;
            UpdateDisplay();
            return;
        }

        if (_current == "0") return;

        if (_current == "-0") _current = "0";
        else _current = _current.StartsWith('-') ? _current[1..] : "-" + _current;

        if (_fonctionTexte != null) _fonctionTexte = $"−({_fonctionTexte})";
        UpdateDisplay();
    }

    private void OnPercent(object? sender, EventArgs e)
    {
        if (_error) return;

        if (_apresEgal)
        {
            _apresEgal = false;
            _expression = "";
            _expressionFinale = "";
            _op = null;
            _left = null;
        }

        double v = Lire(_current);

        // 200 + 10 % => 10 % de 200 = 20 ; sinon simple division par 100
        if (_left != null && (_op == "+" || _op == "-"))
            v = _left.Value * v / 100.0;
        else
            v /= 100.0;

        _current = Formater(v);
        _fonctionTexte = null;
        _newEntry = false;
        UpdateDisplay();
    }

    //  Calcul
    private bool Calculer()
    {
        double a = _left ?? 0;
        double b = Lire(_current);
        double r;

        switch (_op)
        {
            case "+": r = a + b; break;
            case "-": r = a - b; break;
            case "*": r = a * b; break;
            case "/":
                if (b == 0)
                {
                    AfficherErreur("Division par zéro impossible");
                    return false;
                }
                r = a / b;
                break;
            case "^": r = Math.Pow(a, b); break;
            default: r = b; break;
        }

        if (double.IsNaN(r) || double.IsInfinity(r))
        {
            AfficherErreur("Résultat invalide");
            return false;
        }

        _left = r;
        _current = Formater(r);
        return true;
    }

    private void AfficherErreur(string message)
    {
        _error = true;
        _left = null;
        _op = null;
        _expression = "";
        _fonctionTexte = null;
        _apresEgal = false;
        _newEntry = true;
        ExpressionLabel.Text = message;
        ResultLabel.Text = "Erreur";
    }

    private void ResetAll()
    {
        _current = "0";
        _left = null;
        _op = null;
        _newEntry = true;
        _error = false;
        _apresEgal = false;
        _expression = "";
        _expressionFinale = "";
        _fonctionTexte = null;
    }

    //  Affichage et utilitaires
    private string OperandeEnCours() => _fonctionTexte ?? Affiche(_current);

    private void UpdateDisplay()
    {
        if (_error) return;

        ResultLabel.Text = Affiche(_current);

        if (_apresEgal)
        {
            ExpressionLabel.Text = _expressionFinale;
        }
        else if (_expression.Length == 0 && _fonctionTexte == null)
        {
            ExpressionLabel.Text = "";
        }
        else
        {
            // Juste apres un operateur, le second nombre n'est pas encore saisi
            bool operandeAbsent = _newEntry && _fonctionTexte == null && _expression.Length > 0;
            ExpressionLabel.Text = _expression + (operandeAbsent ? "" : OperandeEnCours());
        }

        // Fait defiler vers la fin si le nombre est tres long
        Dispatcher.Dispatch(async () =>
            await ResultScroll.ScrollToAsync(ResultLabel.Width, 0, false));
    }

    private static string Symbole(string op) => op switch
    {
        "*" => "×",
        "/" => "÷",
        "-" => "−",
        _ => op
    };

    private static string Affiche(string s) => s.Replace('.', ',');

    private static double Lire(string s) =>
        double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? v : 0;

    private static string Formater(double v)
    {
        v = Math.Round(v, 10);
        if (v == 0) v = 0;   // evite "-0"

        if (Math.Abs(v) >= 1e15)
            return v.ToString("G10", CultureInfo.InvariantCulture);

        return v.ToString("0.##########", CultureInfo.InvariantCulture);
    }
}
