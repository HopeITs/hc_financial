CREATE ALGORITHM=UNDEFINED DEFINER=`hcfi`@`%` SQL SECURITY DEFINER VIEW `budget_date__monthes` AS select `b`.`budget_id` AS `budget_id`,`a`.`month` AS `month`,`b`.`year` AS `year` from (`a` join `b`)
