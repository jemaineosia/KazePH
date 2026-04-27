START TRANSACTION;
ALTER TABLE events ADD "ChallengedUserId" text;

ALTER TABLE events ADD "ChallengedUsername" text;

ALTER TABLE events ADD "CreatorSide" text;

ALTER TABLE events ADD "CreatorStake" numeric;

ALTER TABLE events ADD "OpponentStake" numeric;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260427055125_AddOneVsOneFields', '10.0.4');

COMMIT;

