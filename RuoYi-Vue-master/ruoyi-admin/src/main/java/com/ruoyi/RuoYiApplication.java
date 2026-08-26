package com.ruoyi;

import org.springframework.boot.SpringApplication;
import org.springframework.boot.autoconfigure.SpringBootApplication;
import org.springframework.boot.jdbc.autoconfigure.DataSourceAutoConfiguration;

import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.util.HashMap;
import java.util.Map;
import java.util.stream.Collectors;
import java.util.stream.Stream;

/**
 * 启动程序
 *
 * @author ruoyi
 */
@SpringBootApplication(exclude = { DataSourceAutoConfiguration.class })
public class RuoYiApplication
{
    public static void main(String[] args)
    {
        SpringApplication application = new SpringApplication(RuoYiApplication.class);
        Map<String, Object> defaultProperties = new HashMap<>();
        String additionalConfigLocation = resolveAdditionalConfigLocation();
        if (additionalConfigLocation != null)
        {
            defaultProperties.put("spring.config.additional-location", additionalConfigLocation);
        }
        if (!defaultProperties.isEmpty())
        {
            application.setDefaultProperties(defaultProperties);
        }
        application.run(args);
        System.out.println("(♥◠‿◠)ﾉﾞ  若依启动成功   ლ(´ڡ`ლ)ﾞ  \n" +
                " .-------.       ____     __        \n" +
                " |  _ _   \\      \\   \\   /  /    \n" +
                " | ( ' )  |       \\  _. /  '       \n" +
                " |(_ o _) /        _( )_ .'         \n" +
                " | (_,_).' __  ___(_ o _)'          \n" +
                " |  |\\ \\  |  ||   |(_,_)'         \n" +
                " |  | \\ `'   /|   `-'  /           \n" +
                " |  |  \\    /  \\      /           \n" +
                " ''-'   `'-'    `-..-'              ");
    }

    private static String resolveAdditionalConfigLocation()
    {
        if (RuoYiApplication.class.getClassLoader().getResource("application.yml") != null)
        {
            return null;
        }

        Path moduleResources = Paths.get("src", "main", "resources");
        Path projectResources = Paths.get("ruoyi-admin", "src", "main", "resources");

        String locations = Stream.of(moduleResources, projectResources)
                .filter(path -> Files.exists(path.resolve("application.yml")))
                .map(Path::toAbsolutePath)
                .map(Path::normalize)
                .map(Path::toUri)
                .map(uri -> "optional:" + uri)
                .collect(Collectors.joining(","));

        return locations.isEmpty() ? null : locations;
    }
}
