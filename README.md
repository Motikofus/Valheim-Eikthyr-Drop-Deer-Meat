# Eikthyr Drop Deer Meat — recréation pour Valheim 1.0

Ce mod ajoute un drop de **5 à 10 viandes de cerf crues** (100% de chance) lorsque
le boss **Eikthyr** meurt, **en plus** de son loot vanilla habituel (trophée d'Eikthyr,
cornes dures, etc.) qui reste inchangé.

## Pourquoi il fallait le recréer

Valheim 1.0 est sorti le 9 septembre 2026. Iron Gate ne garantit la compatibilité
d'aucun mod avec cette version : quasiment tous les plugins BepInEx doivent être
recompilés contre les nouvelles DLL du jeu. Ce projet est donc reconstruit à
partir de zéro, prêt à être compilé contre ta version 1.0 locale.

## Compilation

1. Installe le SDK .NET (6.0 ou plus récent) si ce n'est pas déjà fait.
2. Ouvre `EikthyrDropDeerMeat/EikthyrDropDeerMeat.csproj` et remplace le chemin
   `$(VALHEIM_INSTALL_DIR)` par ton dossier d'installation Valheim réel, par exemple :
   ```
   C:\Program Files (x86)\Steam\steamapps\common\Valheim
   ```
   Tu peux soit éditer directement le `.csproj`, soit définir une variable
   d'environnement `VALHEIM_INSTALL_DIR` avant de compiler.
3. Assure-toi d'avoir BepInExPack pour Valheim installé et **à jour pour la 1.0**
   (vérifie sur Thunderstore que la version dispo est bien compatible 1.0 — au
   lancement, BepInEx lui-même doit être republié pour la nouvelle version du jeu).
4. Compile :
   ```
   dotnet build -c Release
   ```
   Le fichier `EikthyrDropDeerMeat.dll` sera généré dans `bin/Release/netstandard2.1/`.

## Installation

1. Copie `EikthyrDropDeerMeat.dll` dans :
   ```
   <Valheim>/BepInEx/plugins/EikthyrDropDeerMeat/
   ```
2. Lance le jeu. Le log BepInEx (console ou `LogOutput.log`) doit afficher :
   ```
   [Info: Eikthyr Drop Deer Meat] Eikthyr Drop Deer Meat v1.0.0 chargé.
   ```
3. Tue Eikthyr en jeu : tu devrais récupérer entre 5 et 10 viandes de cerf en plus
   du loot normal.

## Points à vérifier après la mise à jour 1.0

- **Nom exact du prefab de la viande de cerf.** Le code utilise `"DeerMeat"`
  (déduit du nom interne `CookedDeerMeat` pour la version cuite). Si Iron Gate a
  renommé cet item dans la 1.0 (par exemple à cause du nouveau biome ou d'un
  rééquilibrage de la nourriture), il faudra ajuster la constante
  `DeerMeatPrefabName` dans `Plugin.cs`.
- **Nom exact du prefab d'Eikthyr.** Le patch vérifie que le nom de l'objet
  commence par `"Eikthyr"`. Si Iron Gate a changé la structure de la scène du
  boss, il peut être nécessaire d'ajuster cette vérification.
- Si `ZNetScene.instance.GetPrefab("DeerMeat")` renvoie `null` au chargement, le
  log affichera un avertissement clair te permettant de repérer immédiatement le
  souci plutôt que d'avoir un échec silencieux.

## Personnaliser les quantités

Dans `Plugin.cs`, ajuste simplement ces deux constantes :
```csharp
private const int MinAmount = 5;
private const int MaxAmount = 10;
```

## Packager pour une distribution publique (Nexus / Thunderstore)

Une fois `EikthyrDropDeerMeat.dll` compilé (voir section Compilation ci-dessus),
voici comment préparer le fichier que tu mettras en téléchargement :

### Pour Nexus Mods (ce que tu utilises déjà)
1. Crée un dossier `EikthyrDropDeerMeat` contenant uniquement :
   ```
   EikthyrDropDeerMeat/
     EikthyrDropDeerMeat.dll
   ```
2. Zippe ce dossier (`EikthyrDropDeerMeat.zip`).
3. L'utilisateur final devra extraire ce zip dans :
   ```
   <Valheim>/BepInEx/plugins/
   ```
   de façon à obtenir `BepInEx/plugins/EikthyrDropDeerMeat/EikthyrDropDeerMeat.dll`.
4. Upload ce zip sur ta page Nexus existante comme nouvelle version (0.221.12 → 1.0.0
   par exemple, pour signaler la compatibilité Valheim 1.0).

### Pour Thunderstore (optionnel, plus simple pour les utilisateurs de gestionnaires de mods)
Le fichier `manifest.json` et `CHANGELOG.md` fournis dans ce package sont déjà au
bon format. Il te faut juste :
1. Ajouter une image `icon.png` en **256x256px** à la racine (obligatoire pour Thunderstore).
2. Structurer le zip final ainsi :
   ```
   EikthyrDropDeerMeat.zip
     manifest.json
     README.md
     CHANGELOG.md
     icon.png
     plugins/
       EikthyrDropDeerMeat.dll
   ```
3. Uploader via https://valheim.thunderstore.io/package/create/

## Ce que je n'ai pas pu faire ici

Je n'ai pas pu compiler le `.dll` moi-même : cela nécessite les DLL propriétaires
de Valheim (`assembly_valheim.dll`, etc.), disponibles uniquement dans ton
installation locale du jeu, à laquelle je n'ai pas accès. Une fois compilé chez
toi avec `dotnet build -c Release`, le `.dll` généré est le fichier final à
distribuer — le reste de ce package (manifest, changelog, structure de dossiers)
est déjà prêt pour ça.
