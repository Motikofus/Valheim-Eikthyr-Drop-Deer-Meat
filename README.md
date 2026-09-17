# Eikthyr Drop Deer Meat — recréation pour Valheim 1.0

Ce mod ajoute un drop de **5 à 10 viandes de cerf crues** (100% de chance) lorsque
le boss **Eikthyr** meurt, **en plus** de son loot vanilla habituel (trophée d'Eikthyr,
cornes dures, etc.) qui reste inchangé.

## Pourquoi il fallait le recréer

Valheim 1.0 est sorti le 9 septembre 2026. Iron Gate ne garantit la compatibilité
d'aucun mod avec cette version : quasiment tous les plugins BepInEx doivent être
recompilés contre les nouvelles DLL du jeu. Ce projet est donc reconstruit à
partir de zéro, prêt à être compilé contre ta version 1.0 locale.

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
