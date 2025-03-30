# 📋 PlanifKanban

**PlanifKanban** est une application WPF moderne permettant de gérer, planifier et suivre l'avancement de tâches sous forme de Kanban. Elle propose une visualisation Gantt avancée, des filtres dynamiques, des exports PDF/Excel, une sauvegarde persistante, et bien plus.

---

## 🚀 Fonctionnalités principales

- ✅ Gestion de tâches par colonnes (À faire, En cours, En test, Terminée)
- 🔀 Glisser-déposer intuitif des tâches entre colonnes
- ✏️ Création et modification avec formulaire complet (client, titre, description, dates, temps)
- 📅 Gestion complète des dates : demandée, prévue, début, finalisation
- ⏱ Estimations de temps prévues / réelles (jours/heures)
- 📊 Affichage Gantt multi-échelles (Jour / Semaine / Mois) avec visualisation optimisée
- 📊 Différenciation visuelle des tâches en cours (orange) et planifiées (bleue)
- 🗃 Sauvegarde/chargement local en XML
- 📑 Export PDF du diagramme Gantt avec mise en page professionnelle
- 📈 Export Excel avec feuilles thématiques (tâches en cours, à faire, terminées)
- 🔍 Tri multi-colonnes intelligent dans tous les tableaux
- 🧠 Visualisation intelligente dans la fenêtre d'ordonnancement
- 🔄 Synchronisation optimisée entre les différentes vues
- 🎨 Interface moderne, responsive et intuitive (styles personnalisés)

---

## 🛠 Technologies

- Framework : [.NET 8](https://dotnet.microsoft.com/)
- UI : WPF (XAML, MVVM partiel)
- Langage : C#
- Persistance : XML
- Bibliothèques : 
  - `System.Windows.Controls`
  - `System.Xml.Serialization`
  - `System.Windows.Data` (CollectionView)
  - `ObservableCollection<T>`
  - `PdfSharp` (génération PDF)
  - `ClosedXML` (génération Excel)

---

## 📦 Installation

### Prérequis

- Windows 10/11
- [.NET SDK 8.0+](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)

### Clone du dépôt

```bash
git clone https://github.com/rodrigueantunes/PlanifKanban.git
cd PlanifKanban
```

### Lancement

Ouvrez la release et exécutez.

---

## 🧭 Utilisation

### 🧱 Vue Kanban

- Double-cliquez pour modifier une tâche
- Clic droit pour supprimer ou déplacer dans une autre colonne
- Les tâches avec date de finalisation sont figées
- Les couleurs changent automatiquement :
  - ✅ Vert si finalisée à temps
  - ❌ Rouge si en retard

### 📅 Vue Ordonnancement (`ScheduleWindow`)

- Accessible via le menu ou bouton dédié
- Permet de filtrer par statut
- Tri multi-colonnes optimisé :
  - Colonne "À faire": Tâches avec date d'abord → Tri par HasDueDate → DueDate → Title
  - Colonne "En cours": Tri par HasStartDate → StartDate → Title
  - Colonne "En test": Tri par HasStartDate → StartDate → Title
  - Colonne "Terminée": Tri par HasCompletionDate → CompletionDate → Title
- Affiche la date "SortDate" (Date début ou prévue prioritaire)

### 📈 Vue Gantt

- Affiche toutes les tâches avec date prévue ou de début
- Exclut les tâches finalisées avec une date passée
- Interface intuitive :
  - Double-clic sur une barre pour modifier la tâche
  - Survol avec effet de surbrillance
  - Colonne Client/Tâche pour une meilleure lisibilité
  - Option d'affichage/masquage des descriptions
- Échelles de temps optimisées :
  - **Jours** : Format JJ/MM avec calcul précis des jours ouvrables
  - **Semaines** : Format SXX-AAAA (numéro de semaine et année)
  - **Mois** : Nom du mois et année
- Organisation intelligente :
  - Algorithme avancé pour éviter les chevauchements
  - Gestion des jours ouvrables (exclusion des samedis/dimanches)
  - Alignement correct sur les débuts de semaine et de mois

### 📤 Exports

#### Export PDF du Gantt
- Génération via le bouton "Exporter Gantt"
- Format paysage professionnel avec titre "Planification Gantt Opérationnelle"
- Inclusion de la date et heure d'exportation
- Légende pour différencier les types de tâches
- Adaptation dynamique à l'échelle selon la tâche la plus longue
- Pagination automatique pour les documents multi-pages

#### Export Excel
- Génération via le bouton "Exporter Excel"
- Trois feuilles thématiques :
  - "Tâches prévu-encours-test" (orange)
  - "Tâches à faire" (bleu)
  - "Tâches terminées" (vert)
- Formatage automatique des dates au format français
- Ajustement automatique de la largeur des colonnes

---

## 📁 Structure du projet

```
PlanifKanban/
│
├── Models/                 # Modèles de données (TaskModel, etc.)
├── ViewModels/            # ViewModels (KanbanViewModel, etc.)
├── Views/                 # Fenêtres WPF (MainWindow, ScheduleWindow, GanttWindow)
├── Converters/            # Converters WPF (NullToVisibility, etc.)
├── Utilities/             # Classes utilitaires et helpers
├── Resources/             # Styles, images, ressources
├── App.xaml               # Configuration globale de l'application
├── PlanifKanban.csproj    # Fichier projet
└── README.md              # Ce fichier
```

---

## 💾 Sauvegarde et Chargement

Les tâches sont sauvegardées localement dans un fichier XML :

```csharp
kanbanViewModel.SaveToFile("kanban.xml");
KanbanViewModel loaded = KanbanViewModel.LoadFromFile("kanban.xml");
```

Les tâches sont regroupées par colonne (`TodoTasks`, `InProgressTasks`, etc.)

---

## 📌 Roadmap

- [x] Vue Kanban interactive
- [x] Ordonnancement avec tri dynamique
- [x] Affichage Gantt multi-échelles
- [x] Export PDF du diagramme Gantt
- [x] Export Excel des tâches
- [ ] Filtres avancés par client/projet
- [ ] Statistiques et tableaux de bord
- [ ] Système de notifications pour tâches imminentes
- [ ] Version mobile compagnon

---

## 🧪 Tests

> Aucun framework de test intégré pour le moment. Des tests manuels sont effectués via les UI.

---

## 🤝 Contribuer

Les contributions sont les bienvenues !  
Merci de suivre ces étapes :

1. Fork du repo
2. Créez votre branche : `git checkout -b feature/amélioration`
3. Commit : `git commit -m "Ajout d'une fonctionnalité"`
4. Push : `git push origin feature/amélioration`
5. Pull Request 🚀

---

## 📄 Licence

Ce projet est sous licence MIT

---

## 🙌 Remerciements

- 💙 Merci à tous ceux qui ont testé et fait des retours.
- ✨ Ce projet est né pour améliorer l'organisation des équipes techniques internes.
- 🔧 Contributeurs spéciaux ayant aidé à améliorer les fonctionnalités Gantt.

---

## ✉️ Contact

Pour toute question, amélioration ou bug :  
📧 `rodrigue.antunes@gmail.com`

---

> Fait en WPF – Parce que le Kanban et le Gantt, c'est la vie.

---
