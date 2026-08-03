-- Execute against the RuoYi MySQL database: ry-vue.
-- Creates or repairs the dedicated TPEM administrator account.

START TRANSACTION;

INSERT INTO sys_user (
  dept_id, user_name, nick_name, user_type, email, phonenumber, sex, avatar,
  password, status, del_flag, pwd_update_date, create_by, create_time, remark
)
SELECT
  100, 'tpem_admin', 'TPEM 系统管理员', '00', '', '', '2', '',
  '$2b$10$VyWab8JMYapNBZWMAQ6ouOKmm6zklFite.K8Fr1lRqKsKpO2XclDi',
  '0', '0', NOW(), 'system', NOW(), 'TPEM 独立系统管理员'
WHERE NOT EXISTS (
  SELECT 1 FROM sys_user WHERE user_name = 'tpem_admin'
);

UPDATE sys_user
SET
  password = '$2b$10$VyWab8JMYapNBZWMAQ6ouOKmm6zklFite.K8Fr1lRqKsKpO2XclDi',
  status = '0',
  del_flag = '0',
  pwd_update_date = NOW(),
  update_by = 'system',
  update_time = NOW()
WHERE user_name = 'tpem_admin';

INSERT INTO sys_user_role (user_id, role_id)
SELECT u.user_id, 1
FROM sys_user u
WHERE u.user_name = 'tpem_admin'
  AND NOT EXISTS (
    SELECT 1
    FROM sys_user_role ur
    WHERE ur.user_id = u.user_id AND ur.role_id = 1
  );

COMMIT;

SELECT u.user_id, u.user_name, u.status, u.del_flag, r.role_id, r.role_name, r.role_key
FROM sys_user u
JOIN sys_user_role ur ON ur.user_id = u.user_id
JOIN sys_role r ON r.role_id = ur.role_id
WHERE u.user_name = 'tpem_admin';
