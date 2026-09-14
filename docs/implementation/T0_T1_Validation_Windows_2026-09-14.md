# T0/T1 — Résultat de validation locale Windows

Date de réception : 14 septembre 2026. Statut : **PASS LOCAL — socle de lecture simulée**.

## Provenance

Résultat transmis par l'utilisateur après exécution de `eng/Test-Pilot.ps1`. Cette exécution a eu lieu sur son poste Windows ; elle n'a pas été reproduite dans l'environnement de l'assistant.

- Source : `Texte collé(20260914-215353).txt`.
- SHA-256 du fichier reçu : `8d232713a17785ed426e5e6d2c1a4fa292b796a08facea40a382740fa11008ee`.
- Windows : version `10.0.22631`, architecture `win-x64`.
- SDK .NET : `10.0.400` ; MSBuild : `18.9.6`.
- Branche de travail : `pilot/t0-t1-contracts-process-boundary`.
- Références Git au moment de la réception : plateforme `fd1fb137f6a55318cf74b040bb28579ca2e1488b` ; pilote `3426d25e5e43c7997d3e2176a7b5183efcb2e15c`. Le journal ne contient pas les SHA des checkouts exécutés ; ces références ne constituent pas une attestation indépendante de leur identité exacte.

## Résultats observés

| Vérification | Résultat du journal |
|---|---|
| Découverte du service gRPC Read | PASS |
| Deux clients, même session et même inventaire | PASS |
| Décimales d'usure et absence d'horodatage source fabriqué | PASS |
| Sélection propre à chaque client | PASS |
| Même port HMI utilisable sans gRPC | PASS |
| Client dans un processus séparé, crash et recréation | PASS ; session Machine conservée |
| Refus de version, cible et requête incorrectes | PASS |
| Absence de dépendance HMI vers un runtime concret ; contrats indépendants des fournisseurs | PASS |
| Invalidation du snapshot et de la sélection après panne | PASS |
| Nouvelle incarnation après redémarrage du Core | PASS |
| Compilation de la solution plateforme | Réussite |
| WS-AT04 — opérations et preuve fraîche de complétion | PASS |
| WS-AT11 — autorisation et absence d'effet après refus | PASS |
| P6.2-D — coexistence et recréation Avalonia/Web | PASS OFFLINE |
| Script de préparation des sept paquets | Termine sans erreur puis passe à la restauration du pilote |
| Compilation et consommation des paquets par le pilote | PASS |
| Fin du script global | Validation locale terminée avec succès |

Le scénario de frontière comporte 17 contrôles PASS. La compilation de solution est réussie, mais le journal montre certaines dépendances en Debug pendant la commande Release (Northbound/Gateway, et MagasinOutil.Core côté pilote). Cette preuve ne certifie donc pas une construction intégralement Release ni un packaging produit. Les empreintes des fichiers nupkg ne figurent pas dans le journal.

## Conclusion et portée

La frontière de lecture de T0/T1 est vérifiée en simulation sur Windows : le cœur possède la session Machine, les clients peuvent être recréés sans reconnexion, et le pilote consomme les composants communs sous forme de paquets. Le correctif de découverte gRPC est exercé avec succès. Les trois scénarios de non-régression ciblés passent.

Cette clôture concerne le socle de lecture T0/T1, pas toute la V1. Le contrat préparatoire d'admission durable reste à compléter et qualifier en T2. La substitution effective de SQLite par un autre moteur n'est pas testée ; le contrôle sans gRPC démontre l'utilisation du port sans cet adaptateur, pas la qualification d'un second transport distant.

L'interface Avalonia du magasin reste son prototype autonome. Les comptes locaux produit, licences temporaires, audit durable, commandes distantes, HMI web intégrée, OPC UA/Fleet du pilote, connexion Beckhoff et conformité CRA ne sont pas validés par cette exécution. WS-AT11 vérifie l'autorisation existante de la plateforme, pas le futur système de comptes du pilote.

## Suite

Préparer la proposition groupée T2 : sessions et comptes locaux, permissions et identités de service, licences temporaires, journal d'admission/audit SQLite, reprise et politique en cas d'indisponibilité. Conserver les décisions produit encore ouvertes comme propositions à valider avant leur implémentation. Aucun lancement de pipeline GitHub, aucune fusion et aucune modification PLC ne sont associés à l'enregistrement de cette preuve.
