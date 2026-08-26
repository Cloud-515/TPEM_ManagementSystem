package com.ruoyi.framework.config;

import java.io.IOException;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashSet;
import java.util.List;
import javax.sql.DataSource;
import org.apache.ibatis.io.VFS;
import org.apache.ibatis.session.SqlSessionFactory;
import org.mybatis.spring.SqlSessionFactoryBean;
import org.mybatis.spring.boot.autoconfigure.SpringBootVFS;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;
import org.springframework.core.env.Environment;
import org.springframework.core.io.DefaultResourceLoader;
import org.springframework.core.io.FileSystemResource;
import org.springframework.core.io.Resource;
import org.springframework.core.io.support.PathMatchingResourcePatternResolver;
import org.springframework.core.io.support.ResourcePatternResolver;
import org.springframework.core.type.classreading.CachingMetadataReaderFactory;
import org.springframework.core.type.classreading.MetadataReader;
import org.springframework.core.type.classreading.MetadataReaderFactory;
import org.springframework.util.ClassUtils;
import com.ruoyi.common.utils.StringUtils;

/**
 * Mybatis支持*匹配扫描包
 * 
 * @author ruoyi
 */
@Configuration
public class MyBatisConfig
{
    @Autowired
    private Environment env;

    static final String DEFAULT_RESOURCE_PATTERN = "**/*.class";

    public static String setTypeAliasesPackage(String typeAliasesPackage)
    {
        ResourcePatternResolver resolver = (ResourcePatternResolver) new PathMatchingResourcePatternResolver();
        MetadataReaderFactory metadataReaderFactory = new CachingMetadataReaderFactory(resolver);
        List<String> allResult = new ArrayList<String>();
        try
        {
            for (String aliasesPackage : typeAliasesPackage.split(","))
            {
                List<String> result = new ArrayList<String>();
                aliasesPackage = ResourcePatternResolver.CLASSPATH_ALL_URL_PREFIX
                        + ClassUtils.convertClassNameToResourcePath(aliasesPackage.trim()) + "/" + DEFAULT_RESOURCE_PATTERN;
                Resource[] resources = resolver.getResources(aliasesPackage);
                if (resources != null && resources.length > 0)
                {
                    MetadataReader metadataReader = null;
                    for (Resource resource : resources)
                    {
                        if (resource.isReadable())
                        {
                            metadataReader = metadataReaderFactory.getMetadataReader(resource);
                            try
                            {
                                result.add(Class.forName(metadataReader.getClassMetadata().getClassName()).getPackage().getName());
                            }
                            catch (ClassNotFoundException e)
                            {
                                e.printStackTrace();
                            }
                        }
                    }
                }
                if (result.size() > 0)
                {
                    HashSet<String> hashResult = new HashSet<String>(result);
                    allResult.addAll(hashResult);
                }
            }
            if (allResult.size() > 0)
            {
                typeAliasesPackage = String.join(",", (String[]) allResult.toArray(new String[0]));
            }
            else
            {
                throw new RuntimeException("mybatis typeAliasesPackage 路径扫描错误,参数typeAliasesPackage:" + typeAliasesPackage + "未找到任何包");
            }
        }
        catch (IOException e)
        {
            e.printStackTrace();
        }
        return typeAliasesPackage;
    }

    public Resource[] resolveMapperLocations(String[] mapperLocations)
    {
        ResourcePatternResolver resourceResolver = new PathMatchingResourcePatternResolver();
        List<Resource> resources = new ArrayList<Resource>();
        if (mapperLocations != null)
        {
            for (String mapperLocation : mapperLocations)
            {
                try
                {
                    Resource[] mappers = resourceResolver.getResources(mapperLocation);
                    resources.addAll(Arrays.asList(mappers));
                    if (mappers.length == 0)
                    {
                        resources.addAll(resolveFallbackMapperLocations(mapperLocation));
                    }
                }
                catch (IOException e)
                {
                    resources.addAll(resolveFallbackMapperLocations(mapperLocation));
                }
            }
        }
        return resources.toArray(new Resource[resources.size()]);
    }

    private List<Resource> resolveFallbackMapperLocations(String mapperLocation)
    {
        List<Resource> resources = new ArrayList<Resource>();
        String relativePath = mapperLocation;
        if (relativePath.startsWith(ResourcePatternResolver.CLASSPATH_ALL_URL_PREFIX))
        {
            relativePath = relativePath.substring(ResourcePatternResolver.CLASSPATH_ALL_URL_PREFIX.length());
        }
        if (relativePath.startsWith("/"))
        {
            relativePath = relativePath.substring(1);
        }
        relativePath = relativePath.replace("**/", "");

        Path[] candidates = new Path[] {
            Paths.get("ruoyi-system", "src", "main", "resources", relativePath),
            Paths.get("src", "main", "resources", relativePath)
        };

        for (Path candidate : candidates)
        {
            if (Files.exists(candidate))
            {
                resources.add(new FileSystemResource(candidate.toFile()));
            }
        }
        return resources;
    }

    private Resource resolveConfigLocation(String configLocation)
    {
        Resource resource = new DefaultResourceLoader().getResource(configLocation);
        if (resource.exists())
        {
            return resource;
        }

        String relativePath = configLocation;
        if (relativePath.startsWith(ResourcePatternResolver.CLASSPATH_URL_PREFIX))
        {
            relativePath = relativePath.substring(ResourcePatternResolver.CLASSPATH_URL_PREFIX.length());
        }
        if (relativePath.startsWith("/"))
        {
            relativePath = relativePath.substring(1);
        }

        Path[] candidates = new Path[] {
            Paths.get("ruoyi-admin", "src", "main", "resources", relativePath),
            Paths.get("src", "main", "resources", relativePath)
        };

        for (Path candidate : candidates)
        {
            if (Files.exists(candidate))
            {
                return new FileSystemResource(candidate.toFile());
            }
        }

        return resource;
    }

    @Bean
    public SqlSessionFactory sqlSessionFactory(DataSource dataSource) throws Exception
    {
        String typeAliasesPackage = env.getProperty("mybatis.typeAliasesPackage");
        String mapperLocations = env.getProperty("mybatis.mapperLocations");
        String configLocation = env.getProperty("mybatis.configLocation");
        typeAliasesPackage = setTypeAliasesPackage(typeAliasesPackage);
        VFS.addImplClass(SpringBootVFS.class);

        final SqlSessionFactoryBean sessionFactory = new SqlSessionFactoryBean();
        sessionFactory.setDataSource(dataSource);
        sessionFactory.setTypeAliasesPackage(typeAliasesPackage);
        sessionFactory.setMapperLocations(resolveMapperLocations(StringUtils.split(mapperLocations, ",")));
        sessionFactory.setConfigLocation(resolveConfigLocation(configLocation));
        return sessionFactory.getObject();
    }
}