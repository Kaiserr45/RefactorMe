SELECT 
    u.name AS username,
    COUNT(sr30.id) AS surveys_last_30_days,
    COALESCE(SUM(sr30.score), 0) AS total_score_last_30_days,
    COUNT(srAll.id) AS total_surveys,
    COALESCE(SUM(srAll.score), 0) AS total_score
FROM users u
LEFT JOIN survey_results srAll ON srAll.user_id = u.id
LEFT JOIN survey_results sr30 ON sr30.user_id = u.id AND sr30.created_at >= NOW() - INTERVAL '30 days'
GROUP BY u.id, u.name;